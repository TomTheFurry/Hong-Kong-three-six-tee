using System.Threading.Tasks;

using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventHalt", menuName = "ChanceEvent/Halt")]
public class ChanceEventHalt : ChanceEvent
{
    public bool SendToJail = false;
    public int HaltTurns;
    public bool removeIlligalItems = false;

    private Task _animation;
    
    public override void OnCreate(ChanceEventState r)
    {
        r.Player.HaltTurns += HaltTurns;
        if (removeIlligalItems)
        {
            r.Player.RemoveIllegalItems();
        }

        if (SendToJail)
        {
            _animation = r.Player.MoveToTile(Game.Instance.Board.JailTile);
        }
    }

    public override bool ServerUpdate(ChanceEventState r) => _animation == null || _animation.IsCompleted;
}
