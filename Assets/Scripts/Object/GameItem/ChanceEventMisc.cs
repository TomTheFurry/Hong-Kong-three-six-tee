using System.Linq;

using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventMisc", menuName = "ChanceEvent/Misc")]
public class ChanceEventMisc : ChanceEvent
{
    public enum ActionType
    {
        IncreaseAllTilePriceChance,
        HalfNextMovement,
        LoseRandomTile,
        ShowIllegalItems,
        NoAction,
    }
    public ActionType Action;

    public override void OnCreate(ChanceEventState r)
    {
        switch (Action)
        {
            case ActionType.IncreaseAllTilePriceChance:
                foreach (var p in Game.Instance.Board.OwnershipItems)
                {
                    if (p.Key.Owner != r.Player) continue;
                    p.Key.TilePriceChangeBias += 0.1f;
                }
                break;
            case ActionType.HalfNextMovement:
                r.Player.NextTurnRollPercent /= 2;
                break;
            case ActionType.LoseRandomTile:
                if (r.IsMaster)
                {
                    var list = Game.Instance.Board.OwnershipItems
                        .Where(p => p.Key.Owner == r.Player)
                        .Select(p => p.Key).ToArray();
                    if (list.Length > 0)
                    {
                        OwnableTile tile = list[Random.Range(0, list.Length)];
                        tile.RemoveOwnership();
                        r.SendClientStateEvent("LoseTile", SerializerUtil.Serialize(tile.TileId));
                    }
                    break;
                }
                break;
            case ActionType.ShowIllegalItems:
                foreach (var p in r.Player.Items)
                {
                    if (p.IsIllegal && p.IsHidden)
                    {
                        p.IsHidden = false;
                    }
                }
                break;
            case ActionType.NoAction:
                break;
            default:
                throw new System.NotImplementedException();
        }
    }

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
    {
        if (e is ClientEventStringData ed && ed.Key == "LoseTile")
        {
            int idx = SerializerUtil.Deserialize<int>(ed.Data);
            OwnableTile tile = Game.Instance.Board.Tiles[idx] as OwnableTile;
            tile.RemoveOwnership();
            return false;
        }
        return base.OnClientEvent(r, e);
    }
}
