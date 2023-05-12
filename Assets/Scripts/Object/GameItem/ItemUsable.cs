using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;
using TMPro;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.Assertions;

class StateUsedItemSelectTile : GameStateLeaf, IUseItemState
{
    private readonly StateTurn.StatePlayerAction Parent;
    public readonly ItemUsable.Event Event;

    public GameTile tileSelected;
    public Task AnimationTask;

    public StateUsedItemSelectTile(ItemUsable.Event @event, [NotNull] StateTurn.StatePlayerAction parent) : base(parent)
    {
        Event = @event;
        Parent = parent;

        if (PhotonNetwork.LocalPlayer == parent.Parent.CurrentPlayer.PunConnection)
        {
            TileInteractor.Instance.RequestSelectTile(t => !(Event is 
                    ItemUsable.Event.TileIncreaseLuck or 
                    ItemUsable.Event.TileDoublePassbyFee or
                    ItemUsable.Event.TileHalfPassbyFee)
                || t is OwnableTile)
                .ContinueWith(
                    t =>
                    {
                        Assert.IsTrue(t.IsCompleted);
                        Game.Instance.photonView.RPC(
                            nameof(Game.Instance.ClientTrySelectTile),
                            RpcTarget.MasterClient, t.Result.TileId
                        );

                    }
                );
        }
    }

    public override EventResult ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventSelectTile sTile)
        {
            tileSelected = sTile.Tile;
            return EventResult.Consumed;
        }
        return EventResult.Invalid;
    }

    private Task RunEvent()
    {
        switch (Event)
        {
            case ItemUsable.Event.TilePlaceItem:
                tileSelected.AddTrapItemOnStep = true;
                return Task.CompletedTask;
            case ItemUsable.Event.TileIncreaseLuck:
                ((OwnableTile)tileSelected).TilePriceChangeBias += 0.1;
                return Task.CompletedTask;
            case ItemUsable.Event.TileHaltTurnOnPass:
                tileSelected.HaltTurnOnPass = true;
                return Task.CompletedTask;
            case ItemUsable.Event.Taxi:
                //todo: Make this step by step to tiles, and sub 50 per tile
                return Parent.Parent.CurrentPlayer.MoveToTile(tileSelected);
            case ItemUsable.Event.TileDoublePassbyFee:
                ((OwnableTile)tileSelected).PassbyFeeMultipliers.Add((Parent.Parent.Round.RoundIdx + 12, 2));
                return Task.CompletedTask;
            case ItemUsable.Event.TileHalfPassbyFee:
                ((OwnableTile)tileSelected).PassbyFeeMultipliers.Add((Parent.Parent.Round.RoundIdx + 12, 0.5f));
                return Task.CompletedTask;
            default: throw new System.NotImplementedException();
        }
    }

    public override GameState Update()
    {
        if (tileSelected == null)
            return null;

        if (AnimationTask == null)
        {
            SendClientStateEvent("SelectedTile", SerializerUtil.Serialize(tileSelected.TileId));
            AnimationTask = RunEvent();
            return null;
        }
        else if (AnimationTask.IsCompleted)
        {
            return new GameStateReturn<bool>(Parent, false);
        }
        else
        {
            return null;
        }
    }

    protected override GameState OnClientUpdate(IClientEvent e)
    {
        if (e is ClientEventStringData sData && sData.Key == "SelectedTile")
        {
            tileSelected = Game.Instance.Board.Tiles[SerializerUtil.Deserialize<int>(sData.Data)];
            _ = RunEvent();
            return null;
        }
        return null;
    }
}

class StateUsedItemSelectPlayer : GameStateLeaf, IUseItemState
{
    private readonly StateTurn.StatePlayerAction Parent;
    public readonly ItemUsable.Event Event;

    public GamePlayer PlayerSelected;
    public Task AnimationTask;

    public StateUsedItemSelectPlayer(ItemUsable.Event @event, [NotNull] StateTurn.StatePlayerAction parent) : base(parent)
    {
        Event = @event;
        Parent = parent;

        if (parent.Parent.CurrentPlayer.PunConnection.IsLocal)
        {
            // client. Do select player
            PlayerInteractor.Instance.ChoosePlayer(false).ContinueWith(
                t =>
                {
                    Assert.IsTrue(t.IsCompleted);
                    Assert.IsNotNull(t.Result);
                    Game.Instance.photonView.RPC(nameof(Game.ClientTrySelectPlayer), RpcTarget.MasterClient, t.Result);
                }
            );
        }
    }

    public override EventResult ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventSelectPlayer sPlayer)
        {
            PlayerSelected = sPlayer.TargetPlayer;
            return EventResult.Consumed;
        }
        return EventResult.Invalid;
    }

    private async Task RunEvent()
    {
        switch (Event)
        {
            case ItemUsable.Event.ReportPlayer:
                bool illegal = PlayerSelected.RemoveIllegalItems();
                if (illegal)
                {
                    PlayerSelected.HaltTurns++;
                    await PlayerSelected.MoveToTile(Game.Instance.Board.JailTile);
                }
                return;
            case ItemUsable.Event.HaltPlayer:
                PlayerSelected.HaltTurns++;
                return;
            case ItemUsable.Event.SeePlayerItem:
                List<Action> actions = new List<Action>();
                foreach (var item in PlayerSelected.Items)
                {
                    if (item is not GameItem gi) continue; 
                    actions.Add(gi.SetTempVisibleTo(Parent.Parent.CurrentPlayer));
                }
                await Task.Delay(10000);
                foreach (var action in actions)
                {
                    action();
                }
                return;
            case ItemUsable.Event.MakePlayerGoodLuck:
                PlayerSelected.Luck += 0.1f;
                return;
            case ItemUsable.Event.MakePlayerBadLuck:
                PlayerSelected.Luck -= 0.1f;
                return;
            default: throw new System.NotImplementedException();
        }
    }

    public override GameState Update()
    {
        if (PlayerSelected == null)
            return null;

        if (AnimationTask == null)
        {
            SendClientStateEvent("SelectedPlayer", SerializerUtil.Serialize(PlayerSelected.Idx));
            AnimationTask = RunEvent();
            return null;
        }
        else if (AnimationTask.IsCompleted)
        {
            return new GameStateReturn<bool>(Parent, false);
        }
        else
        {
            return null;
        }
    }

    protected override GameState OnClientUpdate(IClientEvent e)
    {
        if (e is ClientEventStringData sData && sData.Key == "SelectedPlayer")
        {
            PlayerSelected = Game.Instance.IdxToPlayer[SerializerUtil.Deserialize<int>(sData.Data)];
            _ = RunEvent();
            return null;
        }
        return null;
    }
}

class StateUsedItemQuick : GameStateLeaf, IUseItemState
{
    private readonly StateTurn.StatePlayerAction Parent;
    public readonly ItemUsable.Event Event;

    public StateUsedItemQuick(ItemUsable.Event @event, [NotNull] StateTurn.StatePlayerAction parent) : base(parent)
    {
        Event = @event;
        Parent = parent;

        switch (Event)
        {
            case ItemUsable.Event.Minibus:
                parent.Parent.CurrentPlayer.NextTurnRollMultiplier *= 2;
                break;
            case ItemUsable.Event.RecieveMoneyFromOthers:
                foreach (var player in Game.Instance.IdxToPlayer)
                {
                    if (player != parent.Parent.CurrentPlayer)
                    {
                        //todo: Make this random from 100 - 500
                        player.Funds -= 100;
                        parent.Parent.CurrentPlayer.Funds += 100;
                    }
                }
                break;
            default: throw new System.NotImplementedException();
        }
    }
    public override EventResult ProcessEvent(RPCEvent e) => EventResult.Invalid;
    public override GameState Update() => new GameStateReturn<bool>(Parent, false);
    protected override GameState OnClientUpdate(IClientEvent e) => null;
}

public abstract class GameItem : ItemBase
{
    public string Name;
    public string Description;
    public bool Illegal;
    public bool CanBuyInShop;

    public VisState VisStateToOthers = VisState.Fogged;
    public VisState VisStateToOthersOnGrab = VisState.Fogged;

    public TextMeshPro Nametag;

    public override bool IsIllegal => Illegal;

    public enum VisState {
        None,
        Visible,
        Fogged,
        Invisible
    }

    protected virtual VisState GetVisState()
    {
        if (CurrentOwner == null)
        {
            return VisState.Visible;
        }
        else if (CurrentOwner.PunConnection.IsLocal)
        {
            return VisState.Visible;
        }
        else if (IsGrabbed)
        {
            return VisStateToOthersOnGrab;
        }
        else
        {
            return VisStateToOthers;
        }
    }

    private VisState _lastVisState = VisState.None;
    private GamePlayer _tempVisibleTo = null;
    

    public void Update() {
        VisState visState;
        if (_tempVisibleTo != null && _tempVisibleTo.PunConnection.IsLocal)
        {
            visState = VisState.Visible;
        }
        else
        {
            visState = GetVisState();
        }

        if (visState != _lastVisState)
        {
            _lastVisState = visState;
            switch (visState)
            {
                case VisState.Visible:
                    SetVisible(true);
                    SetFog(false);
                    break;
                case VisState.Fogged:
                    SetVisible(false);
                    SetFog(true);
                    break;
                case VisState.Invisible:
                    SetVisible(false);
                    SetFog(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (Nametag != null) {
            Nametag.text = Name;
            // make nametag face camera
            Nametag.transform.LookAt(Camera.main.transform); // LookAt() doesn't correct for roll
            Nametag.transform.Rotate(0, 180, 0, Space.Self);
        }
    }

    private void SetFog(bool e)
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.GetComponent<ParticleSystemRenderer>().enabled = e;
    }

    private void SetVisible(bool val)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            r.enabled = val;
        }
    }

    public Action SetTempVisibleTo(GamePlayer player)
    {
        _tempVisibleTo = player;
        return () =>
        {
            if (!this.IsDestroyed()) _tempVisibleTo = null;
        };
    }
}

public class ItemUsable : GameItem {
    public override bool IsUsable => EventToTrigger != Event.TilePlaceItem || TilePlaceItemIsVisible;

    public bool TilePlaceItemIsVisible = false;

    public enum Event
    {
        Minibus,
        RecieveMoneyFromOthers,

        ReportPlayer,
        HaltPlayer,
        SeePlayerItem,
        MakePlayerBadLuck,
        MakePlayerGoodLuck,

        TilePlaceItem,
        TileIncreaseLuck,
        TileHaltTurnOnPass,
        Taxi,
        TileDoublePassbyFee,
        TileHalfPassbyFee,
    }

    public Event EventToTrigger;

    private static bool NeedSelectTile(Event e) => e is Event.TilePlaceItem or Event.TileIncreaseLuck or Event.TileHaltTurnOnPass or Event.Taxi or Event.TileDoublePassbyFee or Event.TileHalfPassbyFee;

    private static bool NeedSelectPlayer(Event e) => e is Event.ReportPlayer or Event.HaltPlayer or Event.SeePlayerItem or Event.MakePlayerBadLuck or Event.MakePlayerGoodLuck;

    protected override VisState GetVisState()
    {
        if (EventToTrigger != Event.TilePlaceItem) return base.GetVisState();

        if (TilePlaceItemIsVisible)
        {
            return base.GetVisState();
        }
        else
        {
            return VisState.Invisible;
        }
    }

    public override IUseItemState GetUseItemState(StateTurn.StatePlayerAction parent)
    {
        if (NeedSelectTile(EventToTrigger))
        {
            return new StateUsedItemSelectTile(EventToTrigger, parent);
        }
        else if (NeedSelectPlayer(EventToTrigger))
        {
            return new StateUsedItemSelectPlayer(EventToTrigger, parent);
        }
        else
        {
            return new StateUsedItemQuick(EventToTrigger, parent);
        }
    }

    protected override bool OnReleasedItem(GamePlayer player)
    {
        if (player != CurrentOwner)
        {
            Debug.LogError("Item released by wrong player");
            return false;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Bounds tableBounds = Game.Instance.Board.UseItemCollider.bounds;
            if (tableBounds.Contains(transform.position))
                Game.Instance.PushRPCEvent(new RPCEventUseItem { GamePlayer = player, Item = this });
        }
        return false;
    }



}