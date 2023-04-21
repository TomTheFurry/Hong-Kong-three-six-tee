using System;
using System.Collections.Generic;
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
    public double StepOnPrice => Price * 0.1 * Level;
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
        if (player.Funds < LevelUpPrice) return false;
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
        OwnershipItem.gameObject.SetActive(true);
        Level = 1;
        OnStepState.Value.future.SetResult(0);
    }
    
    [PunRPC]
    public void GamePlayerLevelUp(PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request buy, but game state is not valid!");
            return;
        }
        if (!GamePlayerCanLevelUp(player))
        {
            Debug.LogError($"Player {player} cannot level up this tile!");
            return;
        }
        player.Funds -= LevelUpPrice;
        Level++;
        OnStepState.Value.future.SetResult(0);
    }

    [PunRPC]
    public void GamePlayerComplete(PhotonMessageInfo info)
    {
        GamePlayer player = info.Sender;
        if (OnStepState == null || OnStepState.Value.currentPlayer != player)
        {
            Debug.LogError($"Player {player} request buy, but game state is not valid!");
            return;
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
        player.Funds -= StepOnPrice;
        Owner.Funds += StepOnPrice;
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
                opts.Add((KeyCode.L, "Level Up"));
            }

            KeyCode option = await DebugKeybind.Instance.ChooseActionTemp(opts);
            switch (option)
            {
                case KeyCode.B:
                    photonView.RPC(nameof(GamePlayerBuy), RpcTarget.All);
                    break;
                case KeyCode.L:
                    photonView.RPC(nameof(GamePlayerLevelUp), RpcTarget.All);
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

    public void Start()
    {
        Assert.IsNotNull(OwnershipItem);
        Assert.IsTrue(OwnershipItem.Tile == this);
        AssetDefiner = FindObjectOfType<TileAssetDefiner>();
    }

    public void RemoveOwnership()
    {
        Assert.IsNotNull(OwnershipItem);
        OwnershipItem.CurrentOwner = null;
        OwnershipItem.gameObject.SetActive(false);
    }
}
