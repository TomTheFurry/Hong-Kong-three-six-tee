using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

public class ChanceTile : GameTile
{
    private ChanceSpawner Spawner => ChanceSpawner.Instance;

    private (GamePlayer currentPlayer, TaskCompletionSource<int> future)? OnStepState = null;
    
    public override bool NeedActionOnEnterTile(GamePlayer player) => false;
    public override bool NeedActionOnExitTile(GamePlayer player) => false;

    public override Task ActionsOnStop(GamePlayer player)
    {
        return OnStep(player);
    }

    private async Task OnStep(GamePlayer player)
    {
        // Debug.Log("OnStep!");
        // OnStepState = (player, new TaskCompletionSource<int>());
        //
        // // sub funds
        // if (Owner != null && Owner != player)
        // {
        //     player.Funds -= StepOnPrice;
        //     Owner.Funds += StepOnPrice;
        //     // TODO: Animation effects
        // }
        //
        // // State machine here.
        // if (PhotonNetwork.LocalPlayer == player.PunConnection)
        // {
        //     // I am the active player
        //     // TODO: Show UI
        //     List<(KeyCode, string)> opts = new List<(KeyCode, string)>();
        //     opts.Add((KeyCode.Return, "Continue"));
        //     if (GamePlayerCanBuy(player))
        //     {
        //         opts.Add((KeyCode.B, "Buy"));
        //     }
        //     if (GamePlayerCanLevelUp(player))
        //     {
        //         opts.Add((KeyCode.L, "Level Up"));
        //     }
        //
        //     KeyCode option = await DebugKeybind.Instance.ChooseActionTemp(opts);
        //     switch (option)
        //     {
        //         case KeyCode.B:
        //             photonView.RPC(nameof(GamePlayerBuy), RpcTarget.All);
        //             break;
        //         case KeyCode.L:
        //             photonView.RPC(nameof(GamePlayerLevelUp), RpcTarget.All);
        //             break;
        //         case KeyCode.Return:
        //             photonView.RPC(nameof(GamePlayerComplete), RpcTarget.All);
        //             break;
        //         default: throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // await OnStepState.Value.future.Task;
        // OnStepState = null;
    }

    public void Start()
    {

    }
}
