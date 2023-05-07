using System.Linq;
using System.Threading.Tasks;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

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
                    Assert.IsTrue(t.IsCompleted);
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
