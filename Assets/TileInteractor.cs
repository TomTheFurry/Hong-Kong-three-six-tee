using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Board))]
[DefaultExecutionOrder(-1)]
public class TileInteractor : MonoBehaviour
{
    public static TileInteractor Instance;

    public TileDataSetter InfoUI;

    public float ScalerByDist = 0.5f;

    private Board Board;
    private List<GameTile> Tiles => Board.Tiles;
    private TaskCompletionSource<GameTile> SelectTileTcs; // todo

    void Start()
    {
        Assert.IsNull(Instance);
        Board = GetComponent<Board>();
        Instance = this;
    }

    public void Update()
    {
        InfoUI.gameObject.SetActive(false);
    }

    public void OnClick(GameTile tile)
    {
        if (SelectTileTcs != null)
        {
            SelectTileTcs.SetResult(tile);
            SelectTileTcs = null;
        }
    }

    public Task<GameTile> RequestSelectTile(Func<GameTile, bool> predicate)
    {
        Assert.IsNull(SelectTileTcs);
        SelectTileTcs = new TaskCompletionSource<GameTile>();
        return SelectTileTcs.Task;
    }

    public void OnHover(GameTile tile, Transform camera)
    {
        InfoUI.gameObject.SetActive(true);
        InfoUI.Set(tile);
        InfoUI.transform.position = tile.transform.position + Vector3.up * 2f;
        
        var cameraPos = camera.position;
        var tilePos = tile.transform.position;
        var dir = (tilePos - cameraPos).normalized;
        var dist = Vector3.Distance(cameraPos, tilePos);
        var targetPos = cameraPos + dir * dist * 0.5f;
        InfoUI.transform.localScale = Vector3.one * dist * ScalerByDist;
        InfoUI.transform.LookAt(targetPos);
    }
}
