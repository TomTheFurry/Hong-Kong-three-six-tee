using JetBrains.Annotations;

public class RoundData
{
    public static Game Game => Game.Instance;

    public int RoundIdx = 0;
    public int ActiveOrderIdx;
    [NotNull]
    public StateTurn CurrentTurnState;
    
    // public static StateRound StateRound {
    //     get {
    //         Debug.Log($"Round -- Player: {Game.ActionPlayer.PunConnection.NickName} action");
    //         return _stateRound;
    //     }
    // }

    public RoundData()
    {
        ActiveOrderIdx = 0;
        foreach (GamePlayer player in Game.Instance.JoinedPlayers)
        {
            player.GameSetup(Game.Board);
        }
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
        if (ActiveOrderIdx+1 >= Game.IdxToPlayer.Length) return false;
        ActiveOrderIdx++;
        return true;
    }

    public GamePlayer PeekNextPlayer()
    {
        int nextIdx = ActiveOrderIdx + 1 % Game.IdxToPlayer.Length;
        return Game.IdxToPlayer[Game.playerOrder[nextIdx]];
    }

    public void NextRound()
    {
        RoundIdx++;
        ActiveOrderIdx = 0;
    }

    public GameTile ActivePlayerTile => CurrentTurnState.CurrentPlayer.Tile;

    public GameTile GetTileAt(int stepsForward)
    {
        GameTile tile = CurrentTurnState.CurrentPlayer.Tile;
        for (int i = 0; i < stepsForward; i++)
        {
            tile = tile.NextTile;
        }
        return tile;
    }
}
