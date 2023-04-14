using System;
using System.Collections;
using System.Collections.Generic;

using Assets.Scripts;

using Photon.Pun;
using Photon.Realtime;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;


public class Board : MonoBehaviour
{
    public static GameTile StartTile => Instance.StartingTile;
    static Board Instance;
    public GameTile StartingTile = null;
    public Collider UseItemCollider;
    public int TileHash { get; private set; }
    
    public Dictionary<OwnableTile, LandOwnershipItem> OwnershipItems = new Dictionary<OwnableTile, LandOwnershipItem>();
    public List<GameTile> Tiles = new List<GameTile>();

    public GameTile this[int id] => Tiles[id];

    private void Awake()
    {
        Instance = this;
    }

    private void SetupBoard()
    {
        Tiles = new();
        OwnershipItems = new();
        IEnumerator<GameTile> tiles = ScanAllTiles();
        int tileId = 0;
        int tileHash = 0;
        while (tiles.MoveNext())
        {
            GameTile tile = tiles.Current;
            tile.TileId = tileId++;
            Tiles.Add(tile);

            { // Compute hash for network error detection
                tileHash += HashCode.Combine(tile.TileId, tile.GetType().Name, tile.Name);
                IEnumerator<GameTile> connectingTiles = tile.GetNextTiles();
                while (connectingTiles.MoveNext())
                {
                    GameTile connectingTile = connectingTiles.Current;
                    tileHash += HashCode.Combine(tile.TileId, connectingTile.TileId, connectingTile.Name);
                }
            }

            // Setup ownership item
            if (tile is OwnableTile ownable)
            {
                LandOwnershipItem item = ownable.OwnershipItem;
                OwnershipItems.Add(ownable, item);
            }
        }

    }

    void Start()
    {
        SetupBoard();
    }

    private IEnumerator<GameTile> ScanAllTiles()
    {
        HashSet<GameTile> visited = new HashSet<GameTile>();
        Queue<GameTile> queue = new Queue<GameTile>();
        queue.Enqueue(StartingTile);
        visited.Add(StartingTile);
        yield return StartingTile;
        while (queue.Count > 0)
        {
            GameTile tile = queue.Dequeue();
            
            IEnumerator<GameTile> neighbors = tile.GetNextTiles();
            while (neighbors.MoveNext())
            {
                GameTile neighbor = neighbors.Current;
                if (!visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    yield return neighbor;
                }
            }
        }
    }
    
#if UNITY_EDITOR
    public bool ClickThisToEvalTiles = false;
    public bool ClickThisToReverseTiles = false;
    public bool ClickThisToRenameTiles = false;
    public void OnValidate()
    {
        if (ClickThisToEvalTiles)
        {
            ClickThisToEvalTiles = false;
            SetupBoard();
        }
        if (ClickThisToReverseTiles)
        {
            ClickThisToReverseTiles = false;
            ReverseTiles();
        }
        if (ClickThisToRenameTiles)
        {
            ClickThisToRenameTiles = false;
            RenameTiles();
        }
    }
    
    public void OnDrawGizmos()
    {
        if (StartingTile != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(StartingTile.transform.position + Vector3.up * 1f, 0.5f);
        }
    }

    private void ReverseTiles()
    {
        SetupBoard();
        foreach (GameTile tile in Tiles)
        {
            (tile.NextTile, tile.PrevTile) = (tile.PrevTile, tile.NextTile);
        }
        SetupBoard();
    }

    private void RenameTiles()
    {
        SetupBoard();
        int i = 1;
        GameTile[] tiles = Tiles.ToArray();
        foreach (GameTile tile in Tiles)
        {
            tile.gameObject.name = $"Tile_{i++:##}_{tile.Name}";
            var obj = tile.transform.parent;
            tile.transform.parent = null;
            tile.gameObject.transform.parent = obj; // reorder
        }
    }
#endif

}

public partial class Game
{
    [PunRPC]
    public void CheckHash(int hash)
    {
        if (hash != Board.TileHash)
        {
            Debug.LogError($"Tile hash mismatch. Expected: [{Board.TileHash}], vs: [{hash}]. Is game on different build version????");
            PhotonNetwork.Disconnect();
        }
    }

    private void OnPlayerJoinedCheckHash(Player player)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(CheckHash), player, Board.TileHash);
        }
    }
}
