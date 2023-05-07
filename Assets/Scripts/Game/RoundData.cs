using JetBrains.Annotations;

public class RoundData
{
    public static Game Game => Game.Instance;

    public int RoundIdx = 0;
    public int ActiveOrderIdx;

    [NotNull]
    public StateTurn CurrentTurnState;

    public GamePlayer CurrentPlayer => Game.IdxToPlayer[Game.playerOrder[ActiveOrderIdx]];

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

    public void NextPlayer()
    {
        ActiveOrderIdx = (ActiveOrderIdx + 1) % Game.IdxToPlayer.Length;
    }

    public bool IsLastPlayer()
    {
        return ActiveOrderIdx + 1 >= Game.IdxToPlayer.Length;
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
        foreach (var boardOwnershipItem in Game.Board.OwnershipItems)
        {
            boardOwnershipItem.Key.CleanupFeeChanges(RoundIdx);
        }
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
