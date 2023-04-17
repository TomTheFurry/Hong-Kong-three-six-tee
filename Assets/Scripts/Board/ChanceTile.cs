using System.Threading.Tasks;

public class ChanceTile : GameTile
{
    private ChanceSpawner Spawner => ChanceSpawner.Instance;

    private (GamePlayer currentPlayer, TaskCompletionSource<int> future)? OnStepState = null;

    public int ChanceGroup;
    
    public override bool NeedActionOnEnterTile(GamePlayer player) => false;
    public override bool NeedActionOnExitTile(GamePlayer player) => false;

    public override bool ActionsOnStop(GamePlayer player, StateTurn.StateTurnEffects.StateStepOnTile self, out Task t, out Task<GameState> state)
    {
        Task<ChanceCard> card = ChanceSpawner.Instance.AwaitForDrawCard(ChanceGroup, player);
        state = card
            .ContinueWith(
            t => new ChanceEventState(self, t.Result) as GameState, TaskContinuationOptions.ExecuteSynchronously
        );
        t = null;
        return true;
    }
}
