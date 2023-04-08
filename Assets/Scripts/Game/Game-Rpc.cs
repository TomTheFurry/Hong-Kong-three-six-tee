using System.Linq;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

public partial class Game {
    [PunRPC]
    public void StateChangeChooseOrder(Player[] idxPlayers)
    {
        Debug.Log($"Game state changed to 'Choose Order'.");
        if (photonView.IsMine) return; // ignore
        IdxToPlayer = idxPlayers.Select(p => (GamePlayer)p).ToArray();
        for (var i = 0; i < IdxToPlayer.Length; i++)
        {
            IdxToPlayer[i].Idx = i;
        }
        State = new StateChooseOrder();
    }

    [PunRPC]
    void StateChangeNewRound()
    {
        Debug.Log($"Game state changed to 'New Round'.");
        //if (photonView.IsMine) return; // ignore
        State = new StateNewRound();
    }

    [PunRPC]
    void StateChangeRound()
    {
        Debug.Log($"Game state changed to 'Round'.");
        //if (photonView.IsMine) return; // ignore
        State = RoundData.StateRound;
    }

    [PunRPC]
    void StateChangeTurnEnd()
    {
        Debug.Log($"Game state changed to 'Turn End'.");
        //if (photonView.IsMine) return; // ignore
        State = new StateTurnEnd();
    }

    [PunRPC]
    public void PlayerRolledDice(Player player, int dice)
    {
        Debug.Log($"Player {player.NickName} rolled {dice}");
    }

    [PunRPC]
    public void PlayerSelectedPiece(Player player, int pieceIdx)
    {
        Debug.Log($"Player {player.NickName} selected piece {pieceIdx}");
        //var p = Players[IPlayer.From(player)];
        //p.Piece = Game.Instance.Pieces[pieceIdx];
        //p.Piece.Owner = p.Player;
    }

    [PunRPC]
    public void ClientTrySelectPiece(int pieceIdx, PhotonMessageInfo info)
    {
        Debug.Log($"Client {info.Sender} try select piece {pieceIdx}");
        Debug.Assert(photonView.IsMine);
        EventsToProcess.Add(new RPCEventSelectPiece { GamePlayer = info.Sender, PieceTemplate = PiecesTemplate[pieceIdx] });
    }

    [PunRPC]
    public void ClientTryRollDice(int viewId, PhotonMessageInfo info)
    {
        Debug.Log($"Client {info.Sender} try roll dice");
        Debug.Assert(photonView.IsMine);
        EventsToProcess.Add(new RPCEventRollDice {
            GamePlayer = info.Sender, Dice = PhotonNetwork.GetPhotonView(viewId).GetComponent<Dice6>()
        });
    }

}
