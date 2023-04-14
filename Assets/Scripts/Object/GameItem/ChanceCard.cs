using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

public class ChanceCard : ItemBase, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        CurrentOwner = Game.Instance.roundData.CurrentTurnState.CurrentPlayer;
    }

    public override bool IsUsable => false;
    public override UseItemStateBase GetUseItemState(StateTurn.StatePlayerAction parent) => null;

    public int CardId = 0;
}

public abstract class ChanceEvent : ScriptableObject
{
    public abstract void OnCreate(ChanceEventState r);
    public virtual bool Update(ChanceEventState r) => true;
    public virtual bool ClientUpdate(ChanceEventState r, IClientEvent e) => true;
}

public class ChanceEventState : GameStateLeaf
{
    public GamePlayer Player => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.CurrentPlayer;
    public RoundData Data => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.Round;
    private readonly ChanceEvent Ev;
    protected ChanceEventState([NotNull] StateTurn.StateTurnEffects.StateStepOnTile parent, ChanceEvent ev) : base(parent)
    {
        Ev = ev;
    }

    public override EventResult ProcessEvent(RPCEvent e) => EventResult.Deferred;
    public override GameState Update() => Ev.Update(this) ? new GameStateReturn(Parent) : null;
    protected override GameState OnClientUpdate(IClientEvent e) => Ev.ClientUpdate(this, e) ? new GameStateReturn(Parent) : null;
}

public class ChanceEventMoney : ChanceEvent
{
    public float Money;
    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds += Money;
    }
}

public class ChanceEventMoneyMultiplier : ChanceEvent
{
    public float MoneyMul;

    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds *= MoneyMul;
    }
}

public class ChanceEventMoneySpecial : ChanceEvent
{
    public enum Type
    {
        AllOwnedTilesGainOnePass,
        Give100PerOwnedTileToLast,
        OthersSub500,
        Sub100PerOwnedTile,
    }
    public Type EventType;
    
    public override void OnCreate(ChanceEventState r)
    {
        switch (EventType)
        {
            case Type.AllOwnedTilesGainOnePass:
                foreach (var tile in Game.Instance.Board.Tiles)
                {
                    if (tile is OwnableTile ot && ot.Owner == r.Player)
                    {
                        r.Player.Funds += ot.StepOnPrice;
                    }
                }
                break;
            case Type.Give100PerOwnedTileToLast:
                GamePlayer last = Game.Instance.JoinedPlayers.OrderBy(p => p.Funds).Last();
                if (last != r.Player)
                {
                    foreach (var tile in Game.Instance.Board.Tiles)
                    {
                        if (tile is OwnableTile ot && ot.Owner == r.Player)
                        {
                            r.Player.Funds -= 100;
                            last.Funds += 100;
                        }
                    }
                }
                break;
            case Type.OthersSub500:
                foreach (var player in Game.Instance.JoinedPlayers)
                {
                    if (player != r.Player)
                    {
                        player.Funds -= 500;
                    }
                }
                break;
            case Type.Sub100PerOwnedTile:
                foreach (var tile in Game.Instance.Board.Tiles)
                {
                    if (tile is OwnableTile ot && ot.Owner == r.Player)
                    {
                        r.Player.Funds -= 100;
                    }
                }
                break;
            default: throw new System.NotImplementedException();
        }
    }
}

public class ChanceEventHalt : ChanceEvent
{
    [CanBeNull]
    public GameTile TileToJumpTo;
    public int HaltTurns;
    public bool removeIlligalItems = false;
    private Task _animation;
    
    public override void OnCreate(ChanceEventState r)
    {
        r.Player.HaltTurns += HaltTurns;
        if (removeIlligalItems)
        {
            r.Player.RemoveIllegalItems();
        }

        if (TileToJumpTo != null)
        {
            _animation = r.Player.MoveToTile(TileToJumpTo);
        }
    }

    public override bool Update(ChanceEventState r)
    {
        if (_animation != null && !_animation.IsCompleted)
        {
            return false;
        }
        return true;
    }

    public override bool ClientUpdate(ChanceEventState r, IClientEvent e) => false;
}

public class ChanceEventItem : ChanceEvent
{
    [CanBeNull]
    public ItemBase ItemToGain;

    public override void OnCreate(ChanceEventState r)
    {
        if (!r.IsMaster) return;

        if (ItemToGain != null)
        {
            ItemTemplateDefiner.Instance.ServerInstantiateItem(ItemToGain.Id, r.Player);
        }
    }

    public override bool Update(ChanceEventState r)
    {
        if (ItemToGain == null)
        {
            int count = r.Player.Items.Count;
            if (count > 0)
            {
                int index = Random.Range(0, count);
                var item = r.Player.Items.ElementAt(index);
                r.Player.Items.Remove(item);
                item.photonView.ViewID = 0;
                r.SendClientStateEvent("RemoveItem", SerializerUtil.SerializeItem(item));
                Object.Destroy(item.gameObject);
            }
        }
        return true;
    }

    public override bool ClientUpdate(ChanceEventState r, IClientEvent e)
    {
        if (ItemToGain == null)
        {
            if (e is ClientEventStringData ed && ed.Key == "RemoveItem")
            {
                var item = SerializerUtil.DeserializeItem(ed.Data);
                r.Player.Items.Remove(item);
                item.photonView.ViewID = 0;
                Object.Destroy(item.gameObject);
            }
        }
        return true;
    }
}

public class ChanceEventSelectPlayer : ChanceEvent
{
    public enum ActionType
    {
        SendToJail,
        SendToCheck,
    }
    public ActionType Action;

    public override void OnCreate(ChanceEventState r) { }

    public override bool Update(ChanceEventState r)
    {
        if (r.IsMaster)
        {
            r.SendClientStateEvent("SelectPlayer");
        }
        return true;
    }

    public override bool ClientUpdate(ChanceEventState r, IClientEvent e)
    {
        /*if (e is ClientEventStringData ed && ed.Key == "SelectPlayer")
        {
            var player = Game.Instance.JoinedPlayers.First(p => p.PlayerName == ed.Data);
            switch (Action)
            {
                case ActionType.SendToJail:
                    player.HaltTurns += 3;
                    break;
                case ActionType.SendToCheck:
                    player.Funds -= 100;
                    break;
                default: throw new System.NotImplementedException();
            }
            return true;
        }*/
        return false;
    }
}

public class ChanceEventSelectTile : ChanceEvent
{
    public enum ActionType
    {
        IncreasePriceChance,
        MoveTo,
        DecreasePriceChance,
        RemoveOneLevel,
    }
    public ActionType Action;
    public override void OnCreate(ChanceEventState r) { }
}

public class ChanceEventMisc : ChanceEvent
{
    public enum ActionType
    {
        IncreaseAllTilePriceChance,
        HalfNextMovement,
        LoseRandomTile,
        ShowIllegalItems,
        NoAction,
    }
    public ActionType Action;
    public override void OnCreate(ChanceEventState r) { }
}