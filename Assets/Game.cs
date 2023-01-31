using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ExitGames.Client.Photon;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Events;

using Hashtable = ExitGames.Client.Photon.Hashtable;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Player = Photon.Realtime.Player;

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

public class GamePlayer
{
    public enum ControlType
    {
        Unknown, Vr, Pc,
    }
    public readonly Player PunConnection;
    public PlayerObjBase PlayerObj = null;
    public Piece Piece = null;
    public int Idx = -1;
    public ControlType Control = ControlType.Unknown;
    public HashSet<GameObject> Holding = new();

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
}

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
    public Piece PieceTemplate;
    public override void Fail()
    {
        // Drop the rpc
    }

    public void Success(int number)
    {
        Game.Instance.photonView.RPC("PlayerRolledDice", RpcTarget.AllBufferedViaServer, GamePlayer.PunConnection, number);
    }
}

public abstract class GameState
{
    public abstract bool ProcessEvent(RPCEvent e);
    public abstract void Update(ref GameState stateAtomic);
}

public class StateStartup : GameState
{
    public bool CanStart = false;
    public bool MasterSignalStartGame = false;
    
    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventNewPlayer eNewPlayer)
        {
            //Game.Instance.photonView.RPC("PlayerJoined", RpcTarget.AllBufferedViaServer, eNewPlayer.PunPlayer, eNewPlayer.PunPlayer);
            return true;
        }
        if (e is RPCEventSelectPiece eSelectPiece)
        {
            eSelectPiece.Process();
            return true;
        }
        return false;
    }

    public override void Update(ref GameState state)
    {
        Game.Instance.JoinedPlayersLock.EnterUpgradeableReadLock();
        
        CanStart = Game.Instance.JoinedPlayers.Count(p => p.Piece != null && p.PlayerObj != null && p.Control != GamePlayer.ControlType.Unknown) >= 2;
        if (MasterSignalStartGame && CanStart)
        {
            Debug.Log("Starting game...");
            // Kick all players that are not ready
            foreach (var player in Game.Instance.JoinedPlayers)
            {
                if (player.Piece == null || player.PlayerObj == null || player.Control == GamePlayer.ControlType.Unknown)
                {
                    Debug.Log($"Player {player} Kicked: Not ready before master started game");
                    PhotonNetwork.CloseConnection(player);
                }
            }

            // Setup idx for all players
            int idx = 0;
            foreach (var player in Game.Instance.JoinedPlayers)
            {
                player.Idx = idx++;
            }

            Game.Instance.IdxToPlayer = new GamePlayer[idx];
            foreach (var player in Game.Instance.JoinedPlayers)
            {
                Game.Instance.IdxToPlayer[player.Idx] = player;
            }
            Debug.Log($"IdxToPlayer: {string.Join('\n', Game.Instance.IdxToPlayer.Select((i, p) => $"{i}: {p}"))}");
            // Remove unused pieces
            foreach (var piece in Game.Instance.PiecesTemplate)
            {
                if (piece.Owner == null)
                {
                    PhotonNetwork.Destroy(piece.gameObject);
                }
            }

            // Go to next stage (and signal all players)
            state = new StateChooseOrder();
            Game.Instance.photonView.RPC("StateChangeChooseOrder", RpcTarget.AllBufferedViaServer, Game.Instance.IdxToPlayer.Select(p => p.PunConnection).ToArray() as object);
        }
        
        Game.Instance.JoinedPlayersLock.ExitUpgradeableReadLock();
    }
}

public class StateChooseOrder : GameState
{
    public readonly int[] RolledDice;

    public StateChooseOrder()
    {
        RolledDice = new int[Game.Instance.IdxToPlayer.Length];
    }
    
    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            if (RolledDice[eRollDice.GamePlayer.Idx] != 0) return false;
            RolledDice[eRollDice.GamePlayer.Idx] = Random.Range(1, 7);
            eRollDice.Success(RolledDice[eRollDice.GamePlayer.Idx]);
        }
        return false;
    }

    public override void Update(ref GameState state)
    {
        if (RolledDice.All(d => d != 0))
        {
            //nextState = new StateChooseAction();
            //return true;
        }
        // return false;
    }
}


public class Game : MonoBehaviourPun, IInRoomCallbacks, IConnectionCallbacks, IPunPrefabPool, IMatchmakingCallbacks
{
    public static bool IsMaster => Instance.photonView.IsMine;

    public ReaderWriterLockSlim JoinedPlayersLock = new();
    public SortedSet<GamePlayer> JoinedPlayers = new(Comparer<GamePlayer>.Create((a, b) => a.PunConnection.ActorNumber.CompareTo(b.PunConnection.ActorNumber)));
    public GamePlayer[] IdxToPlayer = null;

    public class PlayerState
    {
        public GamePlayer GamePlayer;

        public bool IsDisconnected => GamePlayer.PunConnection.IsInactive;
        public bool IsInitialized => GamePlayer.PlayerObj != null;
        public bool IsPlaying => GamePlayer.Piece != null;
    }

    public static Game Instance;

    public GameState State = null;
    
    public PlayerState[] Players = null;
    
    public IEnumerable<KeyValuePair<int, PlayerState>> PlayersIter => Players.Select((p, i) => new KeyValuePair<int, PlayerState>(i, p)).Where(p => p.Value != null);
    public int PlayerCount { get; private set; } = 0;

    public enum LocalPlayerState
    {
        NotPlaying = 0,
        IsMaster = 1,
        InRoom = 2,
        HasNetworkBody = 4,
        InGame = 8,
    }
    private LocalPlayerState _localState = LocalPlayerState.NotPlaying;
    public static UnityEvent<LocalPlayerState, LocalPlayerState> OnLocalPlayerTypeChanged = new();
    public LocalPlayerState LocalState
    {
        get => _localState;
        set
        {
            if (_localState == value) return;
            var old = _localState;
            _localState = value;
            OnLocalPlayerTypeChanged.Invoke(old, value);
        }
    }

    //
    // public bool FindIdxForPunPlayer(PunConnection punPlayer, out int idx, out PlayerState state, out bool isReconnect)
    // {
    //     idx = -1;
    //     for (var i = 0; i < Players.Length; i++)
    //     {
    //         bool isNull = Players[i] == null;
    //         if (isNull && idx == -1)
    //         {
    //             idx = i;
    //         }
    //         if (!isNull && Players[i].Player.PunConnection == punPlayer)
    //         {
    //             state = Players[i];
    //             isReconnect = true;
    //             idx = i;
    //             return true;
    //         }
    //     }
    //
    //     if (idx == -1)
    //     {
    //         state = null;
    //         isReconnect = false;
    //         Debug.LogWarning("Too many players try to connect to the server!");
    //         return false;
    //     }
    //     
    //     state = Players[idx] = new PlayerState { P = punPlayer };
    //     PlayerCount++;
    //     isReconnect = false;
    //     return true;
    // }

    public ConcurrentBag<RPCEvent> EventsToProcess = new();

    public Piece[] PiecesTemplate;
    
    public Dictionary<string, GameObject> PunPrefabs = new();
    [Serializable]
    public class PrefabEntry
    {
        public string Name;
        public GameObject Prefab;
    }
    [SerializeField] private PrefabEntry[] Prefabs; // Only so we can set them in the inspector

    void Awake()
    {
        foreach (var prefab in Prefabs)
        {
            PunPrefabs[prefab.Name] = prefab.Prefab;
        }
    }
    
    public void Start()
    {
        Instance = this;
        State = new StateStartup();
        PhotonNetwork.PrefabPool = this;
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
            State.Update(ref State);
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        GamePlayer p = new GamePlayer(newPlayer);
        Debug.Log($"Player {p} joined the game");
        JoinedPlayersLock.EnterWriteLock();
        bool v = JoinedPlayers.Add(p);
        JoinedPlayersLock.ExitWriteLock();
        Debug.Assert(v, "Dup player detected!");
        if (photonView.IsMine) EventsToProcess.Add(new RPCEventNewPlayer { GamePlayer = p });
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer.TagObject is not GamePlayer player) return;
        JoinedPlayersLock.EnterWriteLock();
        if (IsMaster && player.Piece != null)
        {
            player.Piece.SetOwner(null);
        }
        if (IsMaster && player.PlayerObj != null)
        {
            Debug.Log("Backup method removing left player obj");
            //PhotonNetwork.Destroy(player.PlayerObj.gameObject); // TODO: Fix these removal for rejoin logic
        }
        try
        {
            bool v = JoinedPlayers.Remove(player);
            Debug.Assert(v, "Player not found!");
        }
        finally
        {
            JoinedPlayersLock.ExitWriteLock();
        }
    }
    
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }

    public void OnCreatedRoom()
    {
    }
    
    public void OnCreateRoomFailed(short returnCode, string message) { }
    
    public void OnJoinedRoom() {
        JoinedPlayersLock.EnterWriteLock();
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            GamePlayer p = new GamePlayer(player);
            bool v = JoinedPlayers.Add(p);
        }
        JoinedPlayersLock.ExitWriteLock();

        var state = LocalPlayerState.InRoom;
        if (IsMaster)
        {
            state |= LocalPlayerState.IsMaster;
        }
        LocalState |= state;

        //TODO: If vr, spawn vr control
        PhotonNetwork.Instantiate("PlayerPc", Vector3.zero, Quaternion.identity);
    }
    
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }

    public void OnLeftRoom()
    {
        if (PhotonNetwork.LocalPlayer.TagObject is GamePlayer player)
        {
            if (player.Piece != null) player.Piece.Owner = null;
            Object.Destroy(player.PlayerObj);
            player.PlayerObj = null;
        }
        PhotonNetwork.LocalPlayer.TagObject = null;
        LocalState = LocalPlayerState.NotPlaying;
    }
    
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            LocalState |= LocalPlayerState.IsMaster;
        }
        else
        {
            LocalState &= ~LocalPlayerState.IsMaster;
        }
    }

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
        if (cause != DisconnectCause.ApplicationQuit &&
            cause != DisconnectCause.DisconnectByClientLogic &&
            cause != DisconnectCause.DisconnectByServerLogic &&
            cause != DisconnectCause.DisconnectByDisconnectMessage &&
            cause != DisconnectCause.DisconnectByOperationLimit)
        {
            Debug.Log($"Retrying connection in 3 seconds...");
            StartCoroutine(RetryConnection());
        }
    }

    private IEnumerator RetryConnection()
    {
        yield return new WaitForSeconds(3);
        Debug.Log($"Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (PunPrefabs.TryGetValue(prefabId, out var prefab))
        {
            if (prefab.activeInHierarchy)
            {
                Debug.LogError($"PunPrefab requires the prefab to be set and saved as inactive. Prefab: [{prefab.name}])");
                prefab.SetActive(false);
            }
            return Instantiate(prefab, position, rotation);
        }
        else
        {
            Debug.LogError($"Prefab [{prefabId}] not found in registered prefabs. Did you forget to register it?");
            return null;
        }
    }

    public void Destroy(GameObject gameObject)
    {
        Object.Destroy(gameObject);
    }

    [PunRPC]
    public void ClientTryRollDice(int pieceIdx, PhotonMessageInfo info)
    {
        Debug.Log($"Client try roll dice");
        Debug.Assert(photonView.IsMine);
        EventsToProcess.Add(new RPCEventRollDice { GamePlayer = info.Sender, PieceTemplate = PiecesTemplate[pieceIdx] });
    }
}
