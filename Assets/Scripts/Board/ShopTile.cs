using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Photon.Pun;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

public class ShopTile : OwnableTile
{
    protected virtual async Task OnStep(GamePlayer player)
    {
        Debug.Log("OnStep!");
        OnStepState = (player, new TaskCompletionSource<int>());
        // sub funds
        if (Owner != null && Owner != player)
        {
            await OnStepPayFees(player);
        }
        else
        {
            await OnStepCanBuyOrUpgrade(player);
        }
        await OnStepState.Value.future.Task;
        OnStepState = null;
    }
}
