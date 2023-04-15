using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Board))]
public class TileInteractor : MonoBehaviour
{
    public static TileInteractor Instance;

    private Board Board;
    private List<GameTile> Tiles => Board.Tiles;
    private TaskCompletionSource<GameTile> SelectTileTcs; // todo

    void Start()
    {
        Assert.IsNull(Instance);
        Board = GetComponent<Board>();
        Instance = this;
    }

    public void OnClick(GameTile tile)
    {
        // todo
    }

    public Task<GameTile> RequestSelectTile(Func<GameTile, bool> predicate)
    {
        Assert.IsNull(SelectTileTcs);
        SelectTileTcs = new TaskCompletionSource<GameTile>();
        return SelectTileTcs.Task;
    }

}
