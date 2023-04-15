using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

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

[DefaultExecutionOrder(-100)]
public partial class Game : IStateRunner
{
    public static bool IsMaster => Instance.photonView.IsMine;

    public ReaderWriterLockSlim JoinedPlayersLock = new();
    public SortedSet<GamePlayer> JoinedPlayers = new(Comparer<GamePlayer>.Create((a, b) => a.PunConnection.ActorNumber.CompareTo(b.PunConnection.ActorNumber)));
    public GamePlayer[] IdxToPlayer = null;
    public Transform Spawnpoints;
    public Transform ChanceCardSpawnpoint;

    [NonSerialized]
    public Transform[] SpawnpointsArr = null;

    public Transform GetSpawnpoint(int id) => SpawnpointsArr[id % SpawnpointsArr.Length];

    public int PlayerCount => IdxToPlayer.Length;

    public class PlayerState
    {
        public GamePlayer GamePlayer;

        public bool IsDisconnected => GamePlayer.PunConnection.IsInactive;
        public bool IsInitialized => GamePlayer.PlayerObj != null;
        public bool IsPlaying => GamePlayer.Piece != null;
    }

    public static Game Instance;

    public GameState State = null;
    public RoundData roundData;
    public Board Board;

    public PlayerState[] Players = null;

    // get the player index which need to action
    public static GamePlayer ActionPlayer => Instance.IdxToPlayer[ActionPlayerIdx];
    public static int ActionPlayerIdx => Instance.playerOrder[Instance.roundData.ActiveOrderIdx];
    public int[] playerOrder = null;
    
    public IEnumerable<KeyValuePair<int, PlayerState>> PlayersIter => Players.Select((p, i) => new KeyValuePair<int, PlayerState>(i, p)).Where(p => p.Value != null);

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

    // Note: ALWAYS use lock()... on this
    public LinkedList<RPCEvent> EventsToProcess = new();
    public LinkedList<(string[], IClientEvent)> ClientEventsToProcess = new();

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

        SpawnpointsArr = new Transform[Spawnpoints.childCount];
        for (var i = 0; i < Spawnpoints.childCount; i++)
        {
            SpawnpointsArr[i] = Spawnpoints.GetChild(i);
        }
    }
    
    public void Start()
    {
        Instance = this;
        State = new StateStartup(this);
        PhotonNetwork.PrefabPool = this;
        PhotonNetwork.AddCallbackTarget(this);
        PhotonNetwork.ConnectUsingSettings();
    }

    private GameState ClientCreateState(ClientEventSwitchState s)
    {
        return s.StateType switch
        {
            nameof(StateStartup) => new StateStartup(this),
            nameof(StateRollOrder) => new StateRollOrder(this),
            nameof(StateTurn) => new StateTurn(this, roundData),
            _ => throw new Exception($"Unknown state type {s.StateType}")
        };
    }

    private void OnClientEvent(IClientEvent e)
    {
        Debug.LogError($"Unknown event {e}");
    }

    public void LateUpdate()
    {
        if (State == null || State is GameStateReturn) return; // not running

        if (photonView.IsMine)
        {
            lock (EventsToProcess)
            {
                var node = EventsToProcess.First;
                while (node != null)
                {
                    var next = node.Next;
                    var e = node.Value;
                    var result = State.ProcessEvent(e);
                    switch (result)
                    {
                        case EventResult.Consumed:
                            EventsToProcess.Remove(node);
                            break;
                        case EventResult.Invalid:
                            EventsToProcess.Remove(node);
                            e.Fail();
                            break;
                        case EventResult.Deferred:
                            break;
                    }
                    node = next;
                }
            }
            var nextState = State.Update();
            if (nextState != null) State = nextState;
        }
        else
        {
            lock (ClientEventsToProcess)
            {
                var node = ClientEventsToProcess.First;
                while (node != null)
                {
                    var next = node.Next;
                    var (tree, e) = node.Value;

                    GameState nextState = null;
                    if (tree.Length != 0)
                    {
                        Assert.AreEqual(State.GetType().Name, tree[0]);
                        nextState = State.ClientUpdate(tree[1..], e);
                    }
                    else if (e is ClientEventSwitchState stateEvent)
                    {
                        nextState = ClientCreateState(stateEvent);
                    }
                    else
                    {
                         OnClientEvent(e);
                    }
                    if (nextState != null) State = nextState;
                    ClientEventsToProcess.Remove(node);
                    node = next;
                }
            }
        }
    }

    bool IStateRunner.IsMaster => IsMaster;

    public void SendClientStateEvent(IEnumerable<string> tree, IClientEvent e)
    {
        string[] treeArr = tree.ToArray();
        Debug.Log($"ClientStateEvent sent at [{treeArr}]: {e}");
        int typeId = e switch
        {
            ClientEventSwitchState _ => 0,
            ClientEventString _ => 1,
            ClientEventStringData _ => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
        };
        string key = e switch
        {
            ClientEventSwitchState s => s.StateType,
            ClientEventString s => s.Key,
            ClientEventStringData s => s.Key,
            _ => throw new ArgumentOutOfRangeException(nameof(e), e, null)
        };
        byte[] data = e switch
        {
            ClientEventSwitchState s => s.ConstructorData,
            ClientEventStringData s => s.Data,
            _ => Array.Empty<byte>(),
        };
        photonView.RPC(nameof(ClientStateEvent), RpcTarget.Others, treeArr, typeId, key, data);
    }

    public void SendClientStateEvent(IClientEvent e) => SendClientStateEvent(Array.Empty<string>(), e);

    [PunRPC]
    public void ClientStateEvent(string[] tree, int typeId, string key, byte[] data)
    {
        IClientEvent e = typeId switch
        {
            0 => new ClientEventSwitchState { ConstructorData = data, StateType = key },
            1 => new ClientEventString { Key = key },
            2 => new ClientEventStringData { Key = key, Data = data },
            _ => throw new ArgumentOutOfRangeException(nameof(typeId), typeId, null)
        };

        Debug.Log($"ClientStateEvent received at [{tree}]: {e}");
        lock (ClientEventsToProcess)
        {
            ClientEventsToProcess.AddLast((tree, e));
        }
    }

    [PunRPC]
    public void SetIdxToPlayer(Player[] idxPlayers)
    {
        Debug.Log($"SetIdxToPlayer Rpc....");
        if (photonView.IsMine) return; // ignore
        IdxToPlayer = idxPlayers.Select(p => (GamePlayer)p).ToArray();
        for (var i = 0; i < IdxToPlayer.Length; i++)
        {
            IdxToPlayer[i].Idx = i;
        }
        Instance.playerOrder = new int[IdxToPlayer.Length];
    }

    [PunRPC]
    public void ClientTrySelectPlayer(Player player, PhotonMessageInfo info)
    {
        GamePlayer p = player;
        Debug.Log($"Client {info.Sender} try select player {p}");
        Debug.Assert(photonView.IsMine);
        lock (EventsToProcess)
        {
            EventsToProcess.AddLast(new RPCEventSelectPlayer { GamePlayer = info.Sender, TargetPlayer = p });
        }
    }

    [PunRPC]
    public void ClientTrySelectTile(int tileId, PhotonMessageInfo info)
    {
        GameTile tile = Game.Instance.Board.Tiles[tileId];
        Assert.IsNotNull(tile);
        Debug.Log($"Client {info.Sender} try select tile {tile}");
        Debug.Assert(photonView.IsMine);
        lock (EventsToProcess)
        {
            EventsToProcess.AddLast(new RPCEventSelectTile { GamePlayer = info.Sender, Tile = tile});
        }
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
        lock (EventsToProcess)
        {
            EventsToProcess.AddLast(new RPCEventSelectPiece { GamePlayer = info.Sender, PieceTemplate = PiecesTemplate[pieceIdx] });
        }
    }


}