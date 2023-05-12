using System.Collections.Generic;
using System.Threading.Tasks;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

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
