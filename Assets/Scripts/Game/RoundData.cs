using JetBrains.Annotations;

using UnityEngine;

public class RoundData
{
    public static Game Game => Game.Instance;

    public int ActiveOrderIdx;
    [NotNull]
    public StateTurn CurrentTurnState;


    //
    // public static StateRound StateRound {
    //     get {
    //         Debug.Log($"Round -- Player: {Game.ActionPlayer.PunConnection.NickName} action");
    //         return _stateRound;
    //     }
    // }

    public RoundData()
    {
        ActiveOrderIdx = 0;
    }

    /// <summary>
    /// Change the action player index to next player.
    /// </summary>
    /// <returns>
    /// True when have next action player<br/>
    /// False when no next action player
    /// </returns>
    public bool NextPlayer()
    {
        ActiveOrderIdx++;
        if (ActiveOrderIdx >= Game.IdxToPlayer.Length)
        {
            ActiveOrderIdx = 0;
            return false;
        }
    }
}
