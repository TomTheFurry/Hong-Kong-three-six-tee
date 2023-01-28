using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ExitGames.Client.Photon;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

// public enum GameState
// {
//     Startup,
//     ChooseOrder,
//     // --- player rounds ---
//     ChooseAction,
//     ApplyAction,
//     Move,
//     TilePassageEffect,
//     TileStayEffect,
//     SummarizeRound,
//     // --- end player rounds ---
//     SummarizeGame,
//     GameEnd
// }

public class TaskPool
{
    private int RunningTaskCount = 0;

    public void AddTask(Task t)
    {
        Interlocked.Increment(ref RunningTaskCount);
        t.ContinueWith(_ => Interlocked.Decrement(ref RunningTaskCount), TaskContinuationOptions.ExecuteSynchronously);
    }

    public bool IsIdle()
    {
        return RunningTaskCount == 0;
    }
}

public abstract class RPCEvent
{
    public abstract void Fail();
}

public class RPCEventNewPlayer : RPCEvent
{
    public Player PunPlayer;
    public override void Fail()
    {
        PhotonNetwork.CloseConnection(PunPlayer);
    }
}

public class RPCEventSelectPiece : RPCEvent
{
    public IPlayer Player;
    public Piece Piece;
    public override void Fail()
    {
        // Drop the rpc
    }

    public void Process()
    {
        if (Piece.Owner != null)
        {
            Fail();
        }
        else
        {
            Piece.Owner = Player;
            // TODO: RPC the player's assigned piece, (and also give ownership to him)
        }
    }
}

public class RPCEventRollDice : RPCEvent
{
    public IPlayer Player;
    public override void Fail()
    {
        // Drop the rpc
    }

    public void Success(int number)
    {
        Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer,  Player.PunPlayer, number);
    }
}

public interface IPlayer
{
    int Idx { get; }
    Player PunPlayer { get; }

    public static IPlayer From(Player punPlayer) => punPlayer.TagObject as IPlayer;
}

public class PlayerWrapper : IPlayer
{
    public int Idx { get; }
    public Player PunPlayer { get; }

    public PlayerWrapper(int idx, Player punPlayer)
    {
        Idx = idx;
        PunPlayer = punPlayer;
    }
}

public abstract class GameState
{
    public TaskPool Tasks = new();
    public abstract bool CanMoveToNextState();
    public abstract bool ProcessEvent(RPCEvent e);
}

public class StateStartup : GameState
{
    public bool IsGameReady => Game.Instance.Players.Count >= 2 && Tasks.IsIdle() && Game.Instance.Players.Values.All(p => p.Piece != null);

    public bool MasterSignalStartGame = false;

    public override bool CanMoveToNextState()
    {
        return MasterSignalStartGame && IsGameReady;
    }

    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventNewPlayer eNewPlayer)
        {
            var idx = Game.Instance.Players.Count;
            Game.Instance.photonView.RPC("PlayerJoined", RpcTarget.AllBufferedViaServer, idx, eNewPlayer.PunPlayer);
            return true;
        }
        if (e is RPCEventSelectPiece eSelectPiece)
        {
            eSelectPiece.Process();
            return true;
        }
        return false;
    }
}

public class StateChooseOrder : GameState
{
    public int[] RolledDice = new int[Game.Instance.Players.Count];
    public override bool CanMoveToNextState()
    {
        return Tasks.IsIdle() && RolledDice.All(d => d != 0);
    }

    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            if (RolledDice[eRollDice.Player.Idx] != 0) return false;
            RolledDice[eRollDice.Player.Idx] = (int)Math.Floor(Random.Range(1.0f, 6.99999f));
            eRollDice.Success(RolledDice[eRollDice.Player.Idx]);
        }
        return false;
    }
}



public class Game : MonoBehaviourPun, IInRoomCallbacks, IConnectionCallbacks
{
    
    public class PlayerState
    {
        public struct V { }
        public IPlayer Player;
        public ConcurrentDictionary<Task, V> AwaitingTasks = new();
        public Piece Piece;

        public Task AddTask(Task t)
        {
            if (t.IsCompleted)
                return t;
            AwaitingTasks[t] = new V();
            return t.ContinueWith(t => AwaitingTasks.Remove(t, out _));
        }

        public bool IsReady()
        {
            return AwaitingTasks.Count == 0;
        }
    }

    public static Game Instance;
    
    public GameState State = null;

    public Dictionary<IPlayer, PlayerState> Players = new();

    public ConcurrentBag<RPCEvent> EventsToProcess = new();

    public Piece[] PiecesTemplate;

    public void Start()
    {
        Instance = this;
        State = new StateStartup();
        PhotonNetwork.AddCallbackTarget(this);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void LateUpdate()
    {
        if (photonView.IsMine)
        {
            while (EventsToProcess.TryTake(out var e))
            {
                if (!State.ProcessEvent(e))
                {
                    e.Fail();
                }
            }
            // TODO: States stuff.
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        EventsToProcess.Add(new RPCEventNewPlayer { PunPlayer = newPlayer });
    }

    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }

    [PunRPC]
    public void PlayerRolledDice(Player player, int dice)
    {
        Debug.Log($"Player {player.NickName} rolled {dice}");
    }

    [PunRPC]
    public void PlayerJoined(Player player, int assignedPlayerIdx)
    {
        Debug.Log($"Player {player.NickName} joined as player {assignedPlayerIdx}");
        var p = new PlayerState();
        p.Player = new PlayerWrapper(assignedPlayerIdx, player);
        Players[p.Player] = p;
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
        Debug.Assert(photonView.IsMine);
        EventsToProcess.Add(new RPCEventSelectPiece { Player = IPlayer.From(info.Sender), Piece = PiecesTemplate[pieceIdx] });
    }

    public void OnConnected()
    {
        Debug.Log("Connection made to Server");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("Connected to master");
        PhotonNetwork.JoinLobby();
    }
    
    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected: {cause}");
    }
    
    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
}
