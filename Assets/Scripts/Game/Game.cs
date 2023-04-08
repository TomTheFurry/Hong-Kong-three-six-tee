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

using UnityEngine;
using UnityEngine.Events;

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


public partial class Game
{
    public static bool IsMaster => Instance.photonView.IsMine;

    public ReaderWriterLockSlim JoinedPlayersLock = new();
    public SortedSet<GamePlayer> JoinedPlayers = new(Comparer<GamePlayer>.Create((a, b) => a.PunConnection.ActorNumber.CompareTo(b.PunConnection.ActorNumber)));
    public GamePlayer[] IdxToPlayer = null;
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

    public PlayerState[] Players = null;

    // get the player index which need to action
    public static GamePlayer ActionPlayer => Instance.IdxToPlayer[ActionPlayerIdx];
    public static int ActionPlayerIdx => Instance.playerOrder[Instance.roundData.actionOrderIdx];
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
}