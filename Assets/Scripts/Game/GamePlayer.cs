using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;


public class GamePlayer
{
    public enum ControlType
    {
        Unknown, Vr, Pc,
    }
    public readonly Player PunConnection;
    public PlayerObjBase PlayerObj = null;
    public Piece Piece = null;
    public GameTile Tile = null;
    public PlayerCan Can = null;
    public int Idx = -1;
    public ControlType Control = ControlType.Unknown;

    public string Name => PunConnection.NickName;
    
    public HashSet<GameObject> Holding = new();
    // Game States
    public LinkedList<ItemBase> Items = new();

    public double Funds = 1000;
    public int HaltTurns = 0;
    public float NextTurnRollPercent = 1;
    public float Luck = 0.7f;

    public GamePlayer(Player punConnection)
    {
        PunConnection = punConnection;
        Debug.Assert(PunConnection.TagObject == null);
        PunConnection.TagObject = this;
    }

    public static implicit operator GamePlayer(Player punPlayer)
    {
        if (punPlayer == null) return null;
        Debug.Assert(punPlayer.TagObject != null && punPlayer.TagObject is GamePlayer, $"Player {punPlayer} has no GamePlayer tag object");
        return (GamePlayer)punPlayer.TagObject;
    }

    public static implicit operator Player(GamePlayer gamePlayerData)
    {
        if (gamePlayerData == null) return null;
        return gamePlayerData.PunConnection;
    }

    public override string ToString() => (PunConnection.IsLocal ? "[Local]" : "") + (PunConnection.IsMasterClient ? "[Master]" : "") + $"[{Idx}] {PunConnection.NickName} ({Control} controlling {Piece})";

    public void GameSetup(Board board)
    {
        Assert.IsNotNull(PlayerObj);
        Assert.IsNotNull(Piece);
        Tile = board.StartingTile;
        Piece.CurrentTile = Tile;
        if (PhotonNetwork.IsMasterClient)
        {
            PlayerCan.Instantiate(this);
        }
    }

    public Task MoveToTile(GameTile nextTile)
    {
        Tile = nextTile;
        return Piece.MoveToTile(nextTile);
    }

    public bool RemoveIllegalItems()
    {
        bool removed = false;
        foreach (var i in Items)
        {
            if (i.IsIllegal)
            {
                removed = true;
                Items.Remove(i);
                i.photonView.ViewID = 0;
                Object.Destroy(i.gameObject);
            }
        }
        return removed;
    }

    public bool HasHeldItemType(HeldItem.Type type)
    {
        return Items.Any(i => i is HeldItem hi && hi.HeldType == type);
    }
}
