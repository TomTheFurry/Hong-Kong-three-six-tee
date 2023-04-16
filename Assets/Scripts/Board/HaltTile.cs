using System.Threading.Tasks;

public class HaltTile : GameTile
{
    public override bool NeedActionOnEnterTile(GamePlayer player) => false;
    public override bool NeedActionOnExitTile(GamePlayer player) => false;

    public override bool ActionsOnStop(GamePlayer player, StateTurn.StateTurnEffects.StateStepOnTile self, out Task t, out Task<GameState> state)
    {
        player.HaltTurns++;
        t = Task.CompletedTask;
        state = null;
        return false;
    }
}