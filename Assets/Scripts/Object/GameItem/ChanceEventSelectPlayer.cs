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
            List<(KeyCode, string)> choices = new List<(KeyCode, string)>();
            int[] map = new int[Game.Instance.PlayerCount - 1];
            const KeyCode START = KeyCode.Alpha1;
            int i = 0;
            foreach (var player in Game.Instance.JoinedPlayers)
            {
                if (player != r.Player && Game.Instance.PlayerCount != 1) // temp DEBUG
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
