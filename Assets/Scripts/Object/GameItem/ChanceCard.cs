using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ChanceCard : PcGrabInteractable, IPunInstantiateMagicCallback
{
    public ChanceEvent Ev;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Ev = ChanceSpawner.Instance.Events[(int)info.photonView.InstantiationData[0]];
    }

    [PunRPC]
    public void Remove()
    {
        photonView.ViewID = 0;
        Object.Destroy(gameObject);
    }
}

public abstract class ChanceEvent : ScriptableObject
{
    [NonSerialized]
    public int Id;

    public int ChanceMuliplier = 0;
    public abstract void OnCreate(ChanceEventState r);
    public virtual bool ServerUpdate(ChanceEventState r) => true;
    public virtual EventResult OnServerEvent(ChanceEventState r, RPCEvent e) => EventResult.Deferred;
    public virtual bool OnClientEvent(ChanceEventState r, IClientEvent e) => throw new System.NotImplementedException();
}

public class ChanceEventState : GameStateLeaf
{
    public GamePlayer Player => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.CurrentPlayer;
    public RoundData Data => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.Round;
    private readonly ChanceEvent Ev;
    private readonly ChanceCard Card;
    public ChanceEventState([NotNull] StateTurn.StateTurnEffects.StateStepOnTile parent, ChanceCard card) : base(parent)
    {
        Card = card;
        Ev = card.Ev;
    }

    public override EventResult ProcessEvent(RPCEvent e) => Ev.OnServerEvent(this, e);

    public override GameState Update()
    {

        if (Ev.ServerUpdate(this))
        {
            Card.photonView.RPC(nameof(Card.Remove), RpcTarget.Others);
            Card.photonView.ViewID = 0;
            Object.Destroy(Card.gameObject);
            return new GameStateReturn(Parent);
        }
        return null;
    }

    protected override GameState OnClientUpdate(IClientEvent e) => Ev.OnClientEvent(this, e) ? new GameStateReturn(Parent) : null;
}

[CreateAssetMenu(fileName = "ChanceEventMoney", menuName = "ChanceEvent/Money")]
public class ChanceEventMoney : ChanceEvent
{
    public float Money;
    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds += Money;
    }
}

[CreateAssetMenu(fileName = "ChanceEventMoneyMultiplier", menuName = "ChanceEvent/MoneyMultiplier")]
public class ChanceEventMoneyMultiplier : ChanceEvent
{
    public float MoneyMul;

    public override void OnCreate(ChanceEventState r)
    {
        r.Player.Funds *= MoneyMul;
    }
}

[CreateAssetMenu(fileName = "ChanceEventMoneySpecial", menuName = "ChanceEvent/MoneySpecial")]
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

[CreateAssetMenu(fileName = "ChanceEventHalt", menuName = "ChanceEvent/Halt")]
public class ChanceEventHalt : ChanceEvent
{
    [CanBeNull]
    [SerializeReference]
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

    public override bool ServerUpdate(ChanceEventState r) => _animation == null || _animation.IsCompleted;
}

[CreateAssetMenu(fileName = "ChanceEventItem", menuName = "ChanceEvent/Item")]
public class ChanceEventItem : ChanceEvent
{
    [CanBeNull]
    [SerializeReference]
    public ItemBase ItemToGain;

    public override void OnCreate(ChanceEventState r)
    {
        if (!r.IsMaster) return;

        if (ItemToGain != null)
        {
            ItemTemplateDefiner.Instance.ServerInstantiateItem(ItemToGain.Id, r.Player);
        }
    }

    public override bool ServerUpdate(ChanceEventState r)
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

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
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

[CreateAssetMenu(fileName = "ChanceEventSelectPlayer", menuName = "ChanceEvent/SelectPlayer")]
public class ChanceEventSelectPlayer : ChanceEvent
{
    public enum ActionType
    {
        SendToJail,
        SendToICAC
    }
    public ActionType Action;

    private GamePlayer SelectedPlayer = null;
    private Task _animation;

    public override void OnCreate(ChanceEventState r)
    {
        if (r.Player.PunConnection.IsLocal)
        {
            // client. Do select player
            List<(KeyCode, string)> choices = new List<(KeyCode, string)>();
            int[] map = new int[Game.Instance.PlayerCount - 1];
            const KeyCode START = KeyCode.Alpha1;
            int i = 0;
            foreach (var player in Game.Instance.JoinedPlayers)
            {
                if (player != r.Player)
                {
                    choices.Add((START+i, player.Name));
                    map[i++] = player.Idx;
                }
            }
            DebugKeybind.Instance.ChooseActionTemp(choices).ContinueWith(
                t =>
                {
                    var selectedPlayer = Game.Instance.IdxToPlayer[map[t.Result - START]];
                    Assert.IsNotNull(selectedPlayer);
                    Game.Instance.photonView.RPC(nameof(Game.ClientTrySelectPlayer), RpcTarget.MasterClient, selectedPlayer);
                }
            );
        }
    }

    public override EventResult OnServerEvent(ChanceEventState r, RPCEvent e)
    {
        if (e is RPCEventSelectPlayer sp)
        {
            if (sp.GamePlayer != r.Player) return EventResult.Invalid;
            SelectedPlayer = sp.TargetPlayer;
            r.SendClientStateEvent("SelectedPlayer", SerializerUtil.Serialize(SelectedPlayer.Idx));
            return EventResult.Consumed;
        }
        return base.OnServerEvent(r, e);
    }

    private Task DoEvent(ChanceEventState r, GamePlayer target)
    {
        switch (Action)
        {
            case ActionType.SendToJail:
                target.HaltTurns += 1;
                return target.MoveToTile(Game.Instance.Board.JailTile);
            case ActionType.SendToICAC:
                target.HaltTurns += 1;
                Task t = target.MoveToTile(Game.Instance.Board.ICACTile);
                return t.ContinueWith(_ =>
                    {
                        bool hasIllegal = target.RemoveIllegalItems();
                        if (hasIllegal)
                        {
                            target.HaltTurns += 1;
                        }
                    }
                );
            default: throw new System.NotImplementedException();
        }

    }

    public override bool ServerUpdate(ChanceEventState r)
    {
        if (SelectedPlayer == null) return false;
        if (_animation == null)
        {
            _animation = DoEvent(r, SelectedPlayer);
            return false;
        }
        return _animation.IsCompleted;
    }

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
    {
        if (e is ClientEventStringData ed && ed.Key == "SelectedPlayer")
        {
            int idx = SerializerUtil.Deserialize<int>(ed.Data);
            SelectedPlayer = Game.Instance.IdxToPlayer[idx];
            _ = DoEvent(r, SelectedPlayer);
            return false; // server will terminate the state automatically.
        }
        return base.OnClientEvent(r, e);
    }
}

[CreateAssetMenu(fileName = "ChanceEventSelectTile", menuName = "ChanceEvent/SelectTile")]
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

    private GameTile SelectedTile = null;
    private Task _animation;
    private bool skipDueToImpossible = false;

    public override void OnCreate(ChanceEventState r)
    {
        if (Action is ActionType.RemoveOneLevel)
        {
            if (Game.Instance.Board.OwnershipItems.All(p => p.Key.Level <= 1))
            {
                skipDueToImpossible = true;
                return;
            }
        }

        if (r.Player.PunConnection.IsLocal)
        {
            // client. Do select tile
            Task<GameTile> t;
            if (Action is ActionType.MoveTo)
            {
                t = TileInteractor.Instance.RequestSelectTile(_ => true); // any tile
            }
            else if (Action is ActionType.RemoveOneLevel)
            {
                t = TileInteractor.Instance.RequestSelectTile(t => t is OwnableTile && (t as OwnableTile).Level > 1);
            }
            else
            {
                t = TileInteractor.Instance.RequestSelectTile(t => t is OwnableTile);
            }
            t.ContinueWith(
                t =>
                {
                    GameTile tile = t.Result;
                    Assert.IsNotNull(tile);
                    Game.Instance.photonView.RPC(nameof(Game.ClientTrySelectTile), RpcTarget.MasterClient, tile.TileId);
                }
            );
        }
    }

    public override EventResult OnServerEvent(ChanceEventState r, RPCEvent e)
    {
        if (e is RPCEventSelectTile st)
        {
            if (st.GamePlayer != r.Player) return EventResult.Invalid;
            r.SendClientStateEvent("SelectedTile", SerializerUtil.Serialize(st.Tile.TileId));
            return EventResult.Consumed;
        }
        return base.OnServerEvent(r, e);
    }

    private Task DoEvent(ChanceEventState r, GameTile target)
    {
        switch (Action)
        {
            case ActionType.MoveTo:
                return r.Player.MoveToTile(target);
            case ActionType.IncreasePriceChance:
                OwnableTile ot = target as OwnableTile;
                ot.TilePriceChangeBias += 0.3f;
                return Task.CompletedTask;
            case ActionType.DecreasePriceChance:
                OwnableTile ot2 = target as OwnableTile;
                ot2.TilePriceChangeBias -= 0.3f;
                return Task.CompletedTask;
            case ActionType.RemoveOneLevel:
                OwnableTile ot3 = target as OwnableTile;
                Assert.IsTrue(ot3.Level > 1);
                ot3.Level -= 1;
                return Task.CompletedTask;
            default: throw new System.NotImplementedException();
        }

    }

    public override bool ServerUpdate(ChanceEventState r)
    {
        if (skipDueToImpossible) return true;
        if (SelectedTile == null) return false;
        if (_animation == null)
        {
            _animation = DoEvent(r, SelectedTile);
            return false;
        }
        return _animation.IsCompleted;
    }

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
    {
        if (e is ClientEventStringData ed && ed.Key == "SelectedTile")
        {
            int idx = SerializerUtil.Deserialize<int>(ed.Data);
            SelectedTile = Game.Instance.Board.Tiles[idx];
            _ = DoEvent(r, SelectedTile);
            return false; // server will terminate the state automatically.
        }
        return base.OnClientEvent(r, e);
    }
}

[CreateAssetMenu(fileName = "ChanceEventMisc", menuName = "ChanceEvent/Misc")]
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

    public override void OnCreate(ChanceEventState r)
    {
        switch (Action)
        {
            case ActionType.IncreaseAllTilePriceChance:
                foreach (var p in Game.Instance.Board.OwnershipItems)
                {
                    if (p.Key.Owner != r.Player) continue;
                    p.Key.TilePriceChangeBias += 0.1f;
                }
                break;
            case ActionType.HalfNextMovement:
                r.Player.HalfNextTurn = true;
                break;
            case ActionType.LoseRandomTile:
                if (r.IsMaster)
                {
                    var list = Game.Instance.Board.OwnershipItems
                        .Where(p => p.Key.Owner == r.Player)
                        .Select(p => p.Key).ToArray();
                    if (list.Length > 0)
                    {
                        OwnableTile tile = list[Random.Range(0, list.Length)];
                        tile.RemoveOwnership();
                        r.SendClientStateEvent("LoseTile", SerializerUtil.Serialize(tile.TileId));
                    }
                    break;
                }
                break;
            case ActionType.ShowIllegalItems:
                foreach (var p in r.Player.Items)
                {
                    if (p.IsIllegal && p.IsHidden)
                    {
                        p.IsHidden = false;
                    }
                }
                break;
            case ActionType.NoAction:
                break;
            default:
                throw new System.NotImplementedException();
        }
    }

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
    {
        if (e is ClientEventStringData ed && ed.Key == "LoseTile")
        {
            int idx = SerializerUtil.Deserialize<int>(ed.Data);
            OwnableTile tile = Game.Instance.Board.Tiles[idx] as OwnableTile;
            tile.RemoveOwnership();
            return false;
        }
        return base.OnClientEvent(r, e);
    }
}
