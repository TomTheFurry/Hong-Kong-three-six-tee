using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

using Task = System.Threading.Tasks.Task;

public class OwnableTile : GameTile
{
    [SerializeReference]
    //[HideInInspector]
    public LandOwnershipItem OwnershipItem;

    [SerializeReference]
    public MeshRenderer Grid;

    [HideInInspector]
    public TileAssetDefiner AssetDefiner;

    public double Price;

    public double TilePriceChangeBias = 1;

    // level 0: not owned. level 1 to 5: owned
    private int LevelImpl = 0;
    [CanBeNull]
    private GameObject Building = null;

    public int Level
    {
        get => LevelImpl;
        set
        {
            if (LevelImpl != value)
            {
                LevelImpl = value;
                if (Building != null)
                {
                    Destroy(Building);
                }
                if (LevelImpl > 0)
                {
                    Building = Instantiate(AssetDefiner.TileLevelBuildingPrefabs[LevelImpl - 1], transform);
                }
            }
        }
    }

    public double LevelUpPrice => Price * 0.2 * Level+1;
    public double StepOnPrice => Price * 0.1 * Level * feeMultiplier;
    // Equal to total cost of buying this tile and all level ups
    public double LiquidatePriceNoHaircut
    {
        get
        {
            double total = Price;
            for (int i = 2; i <= Level; i++)
            {
                total += Price * 0.2 * i;
            }
            return total;
        }
    }

    [CanBeNull]
    public GamePlayer Owner => OwnershipItem.CurrentOwner;
    
    public List<(int,float)> PassbyFeeMultipliers = new List<(int, float)>();
    public float feeMultiplier => PassbyFeeMultipliers.Aggregate(1f, (acc, x) => acc * x.Item2);

    public void CleanupFeeChanges(int currentRound)
    {
        PassbyFeeMultipliers.RemoveAll(x => x.Item1 >= currentRound);
    }

    public bool GamePlayerCanBuy(GamePlayer player)
    {
        if (Owner != null) return false;
        if (player.Funds < Price) return false;
        return true;
    }

    public bool GamePlayerCanLevelUp(GamePlayer player)
    {
        if (Owner != player) return false;
        if (Level >= 5) return false;
        bool hasDiscount = player.HasItem(HeldItem.Type.HalfCostLevelUp);
        double price = LevelUpPrice * (hasDiscount ? 0.5 : 1);
        if (player.Funds < price) return false;
        return true;
    }

    protected (GamePlayer currentPlayer, TaskCompletionSource<int> future)? OnStepState = null;

    [PunRPC]
    public void GamePlayerBuy(PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request buy, but game state is not valid!");
            return;
        }
        if (!GamePlayerCanBuy(player))
        {
            Debug.LogError($"Player {player} cannot buy this tile!");
            return;
        }
        player.Funds -= Price;
        this.Grid.material = player.Piece.Material;
        OwnershipItem.CurrentOwner = player;
        OwnershipItem.transform.position = transform.position + Vector3.up * 2f;
        float yAngle = Vector3.SignedAngle(Vector3.forward, player.Piece.transform.forward, Vector3.up);
        OwnershipItem.transform.rotation = Quaternion.Euler(0, yAngle, 0);
        OwnershipItem.gameObject.SetActive(true);
        Level = 1;
        OnStepState.Value.future.SetResult(0);
    }
    
    [PunRPC]
    public void GamePlayerLevelUp(bool useDiscount, PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request level up, but game state is not valid!");
            return;
        }
        if (!GamePlayerCanLevelUp(player))
        {
            Debug.LogError($"Player {player} cannot level up this tile!");
            return;
        }
        if (!player.HasItem(HeldItem.Type.HalfCostLevelUp) && useDiscount)
        {
            Debug.LogError($"Player {player} does not have discount item!");
            return;
        }

        double cost = LevelUpPrice * (useDiscount ? 0.5 : 1);
        player.Funds -= cost;
        Level++;
        player.RemoveItem(HeldItem.Type.HalfCostLevelUp);
        OnStepState.Value.future.SetResult(0);
    }

    [PunRPC]
    public void GamePlayerComplete(PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request end step, but game state is not valid!");
            return;
        }
        OnStepState.Value.future.SetResult(0);
    }

    [PunRPC]
    public void GamePlayerSkipFeesWithItem(bool useItem, PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request skip fees, but game state is not valid!");
            return;
        }
        bool hasItem = player.HasItem(HeldItem.Type.NoPassbyFee);
        if (!hasItem && useItem)
        {
            Debug.LogError($"Player {player} does not have skip fees item!");
            return;
        }
        if (useItem)
        {
            player.RemoveItem(HeldItem.Type.NoPassbyFee);
        }
        else
        {
            player.Funds -= StepOnPrice;
            Owner.Funds += StepOnPrice;
        }
        OnStepState.Value.future.SetResult(0);
    }

    public override bool NeedActionOnEnterTile(GamePlayer player) => false;
    public override bool NeedActionOnExitTile(GamePlayer player) => false;
    public sealed override bool ActionsOnStop(GamePlayer player, StateTurn.StateTurnEffects.StateStepOnTile self, out Task t, out Task<GameState> state)
    {
        t = OnStep(player);
        state = null;
        return false;
    }

    protected async Task OnStepPayFees(GamePlayer player)
    {
        bool hasItem = player.HasItem(HeldItem.Type.NoPassbyFee);
        if (hasItem)
        {
            OnStepState = (player, new TaskCompletionSource<int>());
            if (player.PunConnection.IsLocal)
            {
                // I am the active player
                KeyCode val = await DebugKeybind.Instance.ChooseActionTemp(new []{(KeyCode.U, "Use item"), (KeyCode.Return, "Don't use item")});
                switch (val)
                {
                    case KeyCode.U:
                        photonView.RPC(nameof(GamePlayerSkipFeesWithItem), RpcTarget.All, true);
                        break;
                    case KeyCode.Return:
                        photonView.RPC(nameof(GamePlayerSkipFeesWithItem), RpcTarget.All, false);
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            await OnStepState.Value.future.Task;
            OnStepState = null;
        }
        else
        {
            player.Funds -= StepOnPrice;
            Owner.Funds += StepOnPrice;
        }
        // TODO: Animation effects
        await Task.Delay(1000);
    }

    protected async Task OnStepCanBuyOrUpgrade(GamePlayer player)
    {
        OnStepState = (player, new TaskCompletionSource<int>());
        // State machine here.
        if (player.PunConnection.IsLocal)
        {
            // I am the active player
            // TODO: Show UI
            List<(KeyCode, string)> opts = new List<(KeyCode, string)>();
            opts.Add((KeyCode.Return, "Continue"));
            if (GamePlayerCanBuy(player))
            {
                opts.Add((KeyCode.B, "Buy"));
            }


            if (GamePlayerCanLevelUp(player))
            {
                bool hasDiscount = player.HasItem(HeldItem.Type.HalfCostLevelUp);
                if (hasDiscount)
                {
                    opts.Add((KeyCode.K, "Level Up (Use Discount)"));
                    if (player.Funds >= LevelUpPrice)
                    {
                        opts.Add((KeyCode.L, "Level Up"));
                    }
                }
                else
                {
                    opts.Add((KeyCode.L, "Level Up"));
                }
            }

            KeyCode option = await DebugKeybind.Instance.ChooseActionTemp(opts);
            switch (option)
            {
                case KeyCode.B:
                    photonView.RPC(nameof(GamePlayerBuy), RpcTarget.All);
                    break;
                case KeyCode.L:
                    photonView.RPC(nameof(GamePlayerLevelUp), RpcTarget.All, false);
                    break;
                case KeyCode.K:
                    photonView.RPC(nameof(GamePlayerLevelUp), RpcTarget.All, true);
                    break;
                case KeyCode.Return:
                    photonView.RPC(nameof(GamePlayerComplete), RpcTarget.All);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        await OnStepState.Value.future.Task;
        OnStepState = null;
    }

    protected virtual async Task OnStep(GamePlayer player)
    {
        Debug.Log("OnStep!");
        // sub funds
        if (Owner != null && Owner != player)
        {
            await OnStepPayFees(player);
        }
        else
        {
            await OnStepCanBuyOrUpgrade(player);
        }
    }
    
    #region UNITY_EDITOR
#if UNITY_EDITOR
    public new void OnValidate()
    {
        base.OnValidate();
        LandOwnershipItem item = GetComponentInChildren<LandOwnershipItem>(true);
        if (item != null)
        {
            OwnershipItem = item;
            OwnershipItem.Tile = this;
        }
        else
        {
            // make this the selected
            Selection.activeGameObject = gameObject;
            Debug.LogError("OwnershipItem is null");
        }
    }
#endif
    #endregion

    public new void Start()
    {
        (this as GameTile).Start();
        Assert.IsNotNull(OwnershipItem);
        Assert.IsTrue(OwnershipItem.Tile == this);
        AssetDefiner = FindObjectOfType<TileAssetDefiner>();

        // Quickly toggle on and off the item as Pun has a bug where
        // it will not reserve the view id until the object is active.
        OwnershipItem.gameObject.SetActive(true);
        OwnershipItem.GetComponent<TileDataSetter>().Set(this);
        OwnershipItem.gameObject.SetActive(false);
    }

    public void RemoveOwnership()
    {
        Assert.IsNotNull(OwnershipItem);
        OwnershipItem.CurrentOwner = null;
        OwnershipItem.gameObject.SetActive(false);
    }
}
