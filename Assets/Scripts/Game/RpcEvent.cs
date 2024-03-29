
using System.Linq;
using System.Threading.Tasks;

using Photon.Pun;

using UnityEngine;

public abstract class RPCEvent
{
    public abstract void Fail();
}

public class RPCEventNewPlayer : RPCEvent
{
    public GamePlayer GamePlayer;
    public override void Fail()
    {
        // Kick the player
        Debug.LogWarning($"Player {GamePlayer} Kicked: Room join condition failed");
        PhotonNetwork.CloseConnection(GamePlayer);
    }
}

public class RPCEventSelectPiece : RPCEvent
{
    public GamePlayer GamePlayer;
    public Piece PieceTemplate;
    public override void Fail()
    {
        // Drop the rpc
    }

    public void Process()
    {
        // TODO: Are pieces exclusive to players? Doing it as if so.
        bool isDup = Game.Instance.JoinedPlayers.Any(p => p.Piece == PieceTemplate);
        
        if (isDup) 
        {
            Fail();
        }
        else
        {
            if (GamePlayer.Piece != null)
            {
                GamePlayer.Piece.Set(null, false); // remove the old piece from the player's control
            }
            PieceTemplate.Set(GamePlayer, true); // set the new piece to the player's control
        }
    }
}

public class RPCEventRollDice : RPCEvent
{
    public GamePlayer GamePlayer;
    public Dice6 Dice;
    public Task<int> RollTask = null;
    
    public override void Fail()
    {
        Debug.Log($"Dice roll for {GamePlayer} is ignored.");
        // Drop it.
    }

    public void Success(int number)
    {
        //Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer, GamePlayer.PunConnection, number);
    }
}

public class RPCEventSelectPlayer : RPCEvent
{
    public GamePlayer GamePlayer;
    public GamePlayer TargetPlayer;
    public override void Fail()
    {
        Debug.Log($"Player select player event for {GamePlayer} is ignored.");
    }
}

public class RPCEventSelectTile : RPCEvent
{
    public GamePlayer GamePlayer;
    public GameTile Tile;
    public override void Fail()
    {
        Debug.Log($"Player select tile event for {GamePlayer} is ignored.");
    }
}

public class RPCEventUseItem : RPCEvent
{
    public GamePlayer GamePlayer;
    public ItemBase Item; // TODO: Define item type

    public override void Fail()
    {
        Debug.Log($"Player use item event for {GamePlayer} is ignored.");
    }
}