using Photon.Pun;
using System.Threading.Tasks;

using Photon.Realtime;

using UnityEditor;
using UnityEngine;

public class SpecialTile : GameTile
{
    public enum Type {
        RollStaw,
        Gamble,
        Law,
        Invest,
    }

    public Type TileType;
    
    public override bool NeedActionOnEnterTile(GamePlayer player) => false;
    public override bool NeedActionOnExitTile(GamePlayer player) => false;


    public sealed override bool ActionsOnStop(GamePlayer player, StateTurn.StateTurnEffects.StateStepOnTile self, out Task t, out Task<GameState> state) {
        switch (TileType)
        {
            case Type.RollStaw:
                t = OnStepRollStaw(player);
                break;
            default:
                Debug.Log("Todo!");
                t = Task.CompletedTask;
                break;
        }
        state = null;
        return false;
    }

    public TaskCompletionSource<bool> DrawStawTask;

    [PunRPC]
    public void DrawStaw(bool goodLuck, Player player)
    {
        GamePlayer p = player;
        if (goodLuck)
        {
            p.Luck += 1;
        }
        else
        {
            p.Luck -= 0.5f;
            if (p.Luck < 0) { p.Luck = 0; }
        }
        Debug.Log($"Player {p} draw staw: {goodLuck}");
        DrawStawTask.SetResult(goodLuck);
    }

    protected virtual async Task OnStepRollStaw(GamePlayer player) {

        DrawStawTask = new();
        await Task.Delay(1000);
        if (PhotonNetwork.IsMasterClient) {
            bool goodLuck = Random.Range(0, 2) == 0;
            photonView.RPC(nameof(DrawStaw), RpcTarget.All, goodLuck, player.PunConnection);
        }
        await DrawStawTask.Task;
        DrawStawTask = null;
        await Task.Delay(1000);
    }
}
