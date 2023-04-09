
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
            GamePlayer.Piece = PieceTemplate;
            PieceTemplate.Set(GamePlayer, true); // set the new piece to the player's control
        }
    }
}

public class RPCEventRollDice : RPCEvent
{
    public GamePlayer GamePlayer;
    public Dice6 Dice;
    public Task<int> RollTask;
    
    public override void Fail()
    {
        // Drop the rpc
    }

    public void Success(int number)
    {
        Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer, GamePlayer.PunConnection, number);
    }
}

public class RPCEventUseItem : RPCEvent
{
    public GamePlayer GamePlayer;
    public object Item; // TODO: Define item type

    public override void Fail()
    {
        // Drop the rpc
    }

    public void Success(int number)
    {
        //Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer, GamePlayer.PunConnection, number);
    }
}

public class RPCEventPieceMove : RPCEvent {
    public GamePlayer GamePlayer;
    public Piece Piece;
    public int MoveStep;

    public override void Fail() {
        // Drop the rpc
    }

    public void Success(int number) {
        //Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer, GamePlayer.PunConnection, number);
    }
}