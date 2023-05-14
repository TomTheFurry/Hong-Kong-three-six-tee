using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineManager : MonoBehaviour
{
    public static OutlineManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        Game g = Game.Instance;

        if (g.State is StateTurn turn)
        {
            foreach (GamePlayer player in g.IdxToPlayer)
            {
                player.Piece.setOutline(1);
                player.Piece.setOutline(player == turn.CurrentPlayer);
            }
            foreach (GameTile tile in g.Board.Tiles)
            {
                tile.setOutline(0);
                tile.setOutline(false);
            }

            if (turn.ChildState is StateTurn.StateTurnEffects effect)
            {
                if (effect.ChildState is StateTurn.StateTurnEffects.StateStepOnTile onTile)
                {
                    turn.CurrentPlayer.Tile.setOutline(true);
                }
            }
        }

        if (TileInteractor.Instance.IsRequireSelect)
        {
            foreach (GameTile tile in TileInteractor.Instance.GetPredicateTiles())
            {
                tile.setOutline(tile == TileInteractor.Instance.TileHovered ? 2 : 0);
                tile.setOutline(true);
            }
        }
        if (PlayerInteractor.Instance.IsRequireSelect)
        {
            foreach (GamePlayer player in PlayerInteractor.Instance.GetPredicatePlayer())
            {
                player.Piece.setOutline(player == PlayerInteractor.Instance.PlayerHovered ? 2 : 1);
                player.Piece.setOutline(true);
            }
        }
        foreach (PlayerUiIcon icon in PlayerUiIcon.Instances)
        {
            icon.setOutline(false);
        }
        if (PlayerUiIcon.PlayerUiIconHovered != null) PlayerUiIcon.PlayerUiIconHovered.setOutline(true);
    }
}
