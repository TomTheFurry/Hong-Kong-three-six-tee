using System.Linq;

using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventMoneySpecial", menuName = "ChanceEvent/MoneySpecial")]
public class ChanceEventMoneySpecial : ChanceEvent
{
    public enum Type
    {
        AllOwnedTilesGainOnePass,
        Give100PerOwnedTileToLast,
        OthersSub500,
        Sub100PerOwnedTile,
    }
    public Type EventType;
    
    public override void OnCreate(ChanceEventState r)
    {
        switch (EventType)
        {
            case Type.AllOwnedTilesGainOnePass:
                foreach (var tile in Game.Instance.Board.Tiles)
                {
                    if (tile is OwnableTile ot && ot.Owner == r.Player)
                    {
                        r.Player.Funds += ot.StepOnPrice;
                    }
                }
                break;
            case Type.Give100PerOwnedTileToLast:
                GamePlayer last = Game.Instance.JoinedPlayers.OrderBy(p => p.Funds).Last();
                if (last != r.Player)
                {
                    foreach (var tile in Game.Instance.Board.Tiles)
                    {
                        if (tile is OwnableTile ot && ot.Owner == r.Player)
                        {
                            r.Player.Funds -= 100;
                            last.Funds += 100;
                        }
                    }
                }
                break;
            case Type.OthersSub500:
                foreach (var player in Game.Instance.JoinedPlayers)
                {
                    if (player != r.Player)
                    {
                        player.Funds -= 500;
                    }
                }
                break;
            case Type.Sub100PerOwnedTile:
                foreach (var tile in Game.Instance.Board.Tiles)
                {
                    if (tile is OwnableTile ot && ot.Owner == r.Player)
                    {
                        r.Player.Funds -= 100;
                    }
                }
                break;
            default: throw new System.NotImplementedException();
        }
    }
}
