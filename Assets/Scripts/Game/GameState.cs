using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using ExitGames.Client.Photon;

using JetBrains.Annotations;

using Newtonsoft.Json.Serialization;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public interface IStateRunner
{
    public bool IsMaster { get; }
    public void SendClientStateEvent(IEnumerable<string> tree, IClientEvent e);
    public void SendClientStateEvent(IClientEvent e);
}

public enum EventResult
{
    Consumed,
    Invalid,
    Deferred
}

public interface IClientEvent
{
}

public class ClientEventSwitchState : IClientEvent
{
    [NotNull]
    public string StateType;
    [NotNull]
    public byte[] ConstructorData;
}

public class ClientEventString : IClientEvent
{
    [NotNull]
    public string Key;
}

public class ClientEventStringData : IClientEvent
{
    [NotNull]
    public string Key;
    [NotNull]
    public byte[] Data;
}

public abstract class GameState : IStateRunner
{
    [NotNull]
    public IStateRunner Parent;
    public bool IsMaster => Parent.IsMaster;
    protected GameState([NotNull] IStateRunner parent) => Parent = parent;

    public abstract EventResult ProcessEvent([NotNull] RPCEvent e);

    [CanBeNull]
    public abstract GameState Update();

    [CanBeNull]
    public abstract GameState ClientUpdate(Span<string> tree, IClientEvent e);

    public void SendClientStateEvent(string key, byte[] data) => SendClientStateEvent(new ClientEventStringData { Key = key, Data = data });
    public void SendClientStateEvent(string key) => SendClientStateEvent(new ClientEventString { Key = key });
    
    public void SendClientStateEvent(IClientEvent e) => SendClientStateEvent(Enumerable.Empty<string>(), e);

    public void SendClientStateEvent(IEnumerable<string> tree, IClientEvent e)
    {
        Parent.SendClientStateEvent(tree.Prepend(GetType().Name), e);
    }

    public void SendClientSetReturnState<T>([CanBeNull] byte[] data = null) where T : GameState
    {
        Parent.SendClientStateEvent(new ClientEventSwitchState
        {
            StateType = typeof(T).Name,
            ConstructorData = data ?? Array.Empty<byte>()
        });
    }
}

public abstract class GameStateLeaf : GameState
{
    protected GameStateLeaf([NotNull] IStateRunner parent) : base(parent) { }

    public sealed override GameState ClientUpdate(Span<string> tree, IClientEvent e)
    {
        Assert.IsTrue(tree.IsEmpty);
        return OnClientUpdate(e);
    }

    [CanBeNull]
    protected abstract GameState OnClientUpdate(IClientEvent e);
}

public class GameStateReturn : GameStateLeaf
{
    public override EventResult ProcessEvent(RPCEvent e) => EventResult.Deferred;
    public override GameState Update() => throw new NotImplementedException();
    protected override GameState OnClientUpdate(IClientEvent e) => throw new NotImplementedException();
    public GameStateReturn([NotNull] IStateRunner parent) : base(parent) { }
}

public class GameStateReturn<TData> : GameStateReturn
{
    public TData Data;
    public GameStateReturn([NotNull] IStateRunner parent, TData data) : base(parent) => Data = data;
}


public abstract class NestedGameState : GameState
{
    [CanBeNull]
    public GameState ChildState;

    protected NestedGameState([NotNull] IStateRunner parent) : base(parent) { }

// return 'null' to not exit this state machine, or return new state to signal up to parent state machine. Also can set ChildState to new state here.
    [NotNull]
    protected abstract GameState OnStateReturnControl([NotNull] GameStateReturn @return);
    
    public abstract EventResult OnSelfProcessEvent(RPCEvent e);
    
// return 'null' to not exit this state machine, or return new state to signal up to parent state machine. Also can set ChildState to new state here.
    // Note: It is invalid to set ChildState to GameStateReturn here!
    [CanBeNull]
    public abstract GameState OnSelfUpdate();

    // process the switch state event, and return the new state object
    [CanBeNull]
    protected abstract GameState ClientCreateState([NotNull] ClientEventSwitchState s);

    // return 'null' to not exit this state machine, or return new state to signal up to parent state machine. Also can set ChildState to new state here.
    [NotNull]
    protected abstract GameState OnClientStateReturnControl([NotNull] GameStateReturn @return);

    // process the client event. Return 'null' to not exit this state machine, or return new state to signal up to parent state machine. Also can set ChildState to new state here.
    // Note: It is invalid to set ChildState to GameStateReturn here!
    [NotNull]
    protected abstract GameState OnClientEvent([NotNull] IClientEvent e);
    
    public void SendClientSetState<T>([CanBeNull] byte[] data = null) where T : GameState
    {
        SendClientStateEvent(new ClientEventSwitchState
        {
            StateType = typeof(T).Name,
            ConstructorData = data ?? Array.Empty<byte>()
        });
    }

    #region Impl
    // Can override
    public override EventResult ProcessEvent(RPCEvent e) => ChildState?.ProcessEvent(e) ?? OnSelfProcessEvent(e);

    // Can override
    public override GameState Update()
    {
        if (ChildState == null) return OnSelfUpdate();

        GameState nextState = ChildState.Update();
        if (nextState == null) return null;
        if (nextState is GameStateReturn exit) return OnStateReturnControl(exit);
        ChildState = nextState;
        return null;
    }
    
    public sealed override GameState ClientUpdate(Span<string> tree, IClientEvent e)
    {
        GameState nextState;
        if (!tree.IsEmpty)
        {
            Assert.AreEqual(ChildState.GetType().Name, tree[0]);
            nextState = ChildState.ClientUpdate(tree.Slice(1), e);
        }
        else if (e is ClientEventSwitchState stateEvent)
        {
            nextState = ClientCreateState(stateEvent);
        }
        else
        {
            return OnClientEvent(e);
        }
        if (nextState == null) return null;
        if (nextState is GameStateReturn exit) return OnClientStateReturnControl(exit);
        ChildState = nextState;
        return null;
    }
    #endregion
}

public class StateStartup : GameStateLeaf
{
    public bool CanStart = false;
    public bool MasterSignalStartGame = false;

    public override EventResult ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventNewPlayer eNewPlayer)
        {
            //Game.Instance.photonView.RPC("PlayerJoined", RpcTarget.AllBufferedViaServer, eNewPlayer.PunPlayer, eNewPlayer.PunPlayer);
            return EventResult.Consumed;
        }
        if (e is RPCEventSelectPiece eSelectPiece)
        {
            eSelectPiece.Process();
            return EventResult.Consumed;
        }
        return EventResult.Invalid;
    }

    public override GameState Update() 
    {
        Game.Instance.JoinedPlayersLock.EnterUpgradeableReadLock();
        try
        {
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

                // Init PlayerOrder
                Game.Instance.playerOrder = new int[idx];

                // Remove unused pieces
                foreach (var piece in Game.Instance.PiecesTemplate)
                {
                    if (piece.Owner == null)
                    {
                        PhotonNetwork.Destroy(piece.gameObject);
                    }
                    else
                    {
                        piece.photonView.RPC(nameof(Piece.UpdataOwnerMaterial), RpcTarget.AllBufferedViaServer);
                        piece.photonView.RPC(nameof(Piece.InitCurrentTile), RpcTarget.AllBufferedViaServer);
                    }
                }

                // Send idx to all players
                Game.Instance.photonView.RPC(nameof(Game.SetIdxToPlayer), RpcTarget.AllBufferedViaServer, Game.Instance.IdxToPlayer.Select(p => p.PunConnection).ToArray() as object);
                // Go to next stage
                SendClientSetReturnState<StateRollOrder>();
                return new StateRollOrder(Parent);
            }
            return null;
        }
        finally
        {
            Game.Instance.JoinedPlayersLock.ExitUpgradeableReadLock();
        }
    }

    protected override GameState OnClientUpdate(IClientEvent e) => null;
    public StateStartup([NotNull] IStateRunner parent) : base(parent) { }
}

public class StateRollOrder : GameStateLeaf
{
    public readonly int[] RollNumByIdx;
    public readonly int[][] RolledDice;
    private LinkedList<RPCEventRollDice> activeRolls = new();

    public StateRollOrder([NotNull] IStateRunner runner) : base(runner)
    {
        RollNumByIdx = new int[Game.Instance.IdxToPlayer.Length];

        RolledDice = new int[Game.Instance.IdxToPlayer.Length][];
        for (int i = 0; i < RolledDice.Length; i++)
        {
            RolledDice[i] = new[] { i, 0 };
        }
    }

    public override EventResult ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            if (RollNumByIdx[eRollDice.GamePlayer.Idx] != 0) return EventResult.Invalid;
            Dice6 dice = eRollDice.Dice;
            GamePlayer player = eRollDice.GamePlayer;
            eRollDice.RollTask = dice.StartRoll(player);
            activeRolls.AddLast(eRollDice);
            return EventResult.Consumed;
        }
        return EventResult.Invalid;
    }

    public override GameState Update()
    {
        { // Process all completed rolls
            var node = activeRolls.First;
            while (node != null)
            {
                var next = node.Next;
                var e = node.Value;
                if (e.RollTask.IsCompleted)
                {
                    if (e.RollTask.IsCompletedSuccessfully && e.RollTask.Result > 0)
                    {
                        RollNumByIdx[e.GamePlayer.Idx] = e.RollTask.Result;
                        e.Success(e.RollTask.Result);
                    }
                    else
                    {
                        e.Fail();
                    }
                    activeRolls.Remove(node);
                }
                node = next;
            }
        }
        // If all rolls are done, compute orders and go to next stage
        if (RolledDice.All(d => d[1] != 0))
        {
            var enumRoll = RollNumByIdx
                .Select((d, idx) => Tuple.Create(d, -idx))
                .OrderByDescending(t => t)
                .Select(t => -t.Item2);
            int[] playerOrders = enumRoll.ToArray();
            int i = 0;
            foreach (int idx in playerOrders)
            {
                Game.Instance.playerOrder[i++] = idx;
            }
            SendClientStateEvent("PlayerOrder", SerializerUtil.SerializeArray(playerOrders));

            // setup game
            Game.Instance.roundData = new RoundData();
            SendClientStateEvent("InitRoundData");

            SendClientSetReturnState<StateTurn>();
            return new StateTurn(Parent, Game.Instance.roundData);
        }
        return null;
    }

    protected override GameState OnClientUpdate(IClientEvent e)
    {
        if (e is ClientEventStringData d && d.Key == "PlayerOrder")
        {
            int[] playerOrders = SerializerUtil.DeserializeArray<int>(d.Data);
            int i = 0;
            foreach (int idx in playerOrders)
            {
                Game.Instance.playerOrder[i++] = idx;
            }
        }
        else if (e is ClientEventString d2 && d2.Key == "InitRoundData")
        {
            Game.Instance.roundData = new RoundData();
        }
        return null;
    }
}

public class StateTurn : NestedGameState
{
    private readonly RoundData Round;
    public readonly int CurrentOrderIdx;
    public int CurrentPlayerIdx => Game.Instance.playerOrder[CurrentOrderIdx];
    [NotNull]
    public GamePlayer CurrentPlayer => Game.Instance.IdxToPlayer[CurrentPlayerIdx];

    public StateTurn([NotNull] IStateRunner parent, [NotNull] RoundData data) : base(parent)
    {
        Round = data;
        Round.CurrentTurnState = this;
        CurrentOrderIdx = Round.ActiveOrderIdx;

    }

    public override GameState OnSelfUpdate()
    {
        // Start of turn.
        SendClientSetState<StatePlayerAction>();
        ChildState = new StatePlayerAction(this);
        return null;
    }
    
    public override EventResult OnSelfProcessEvent(RPCEvent e) => EventResult.Deferred;
    protected override GameState OnClientEvent(IClientEvent e) => null;
    protected override GameState OnStateReturnControl(GameStateReturn @return) => null;
    protected override GameState OnClientStateReturnControl(GameStateReturn @return) => null;
    
    protected override GameState ClientCreateState(ClientEventSwitchState s)
    {
        switch (s.StateType)
        {
            case "PlayerAction":
                return new StatePlayerAction(this);
            default:
                return null;
        }
    }

    public class StatePlayerAction : NestedGameState
    {
        [NotNull]
        public new StateTurn Parent;
        public StatePlayerAction([NotNull] StateTurn parent) : base(parent) => Parent = parent;

        public override GameState OnSelfUpdate()
        {
            // Start of turn.
            SendClientSetState<StateWaitForAction>();
            ChildState = new StateWaitForAction(this);
            return null;
        }

        public override EventResult OnSelfProcessEvent(RPCEvent e) => EventResult.Deferred;
        protected override GameState OnClientEvent(IClientEvent e) => null;
        protected override GameState OnStateReturnControl(GameStateReturn @return) => null;
        protected override GameState OnClientStateReturnControl(GameStateReturn @return) => null;

        protected override GameState ClientCreateState(ClientEventSwitchState s)
        {
            switch (s.StateType)
            {
                case "WaitForAction":
                    return new StateWaitForAction(this);
                default:
                    return null;
            }
        }
        
        public class StateWaitForAction : GameStateLeaf
        {
            [CanBeNull]
            public RPCEventRollDice RollEvent;
            [CanBeNull]
            public object UseItem; // TODO: change type to item type

            [NotNull]
            public new StatePlayerAction Parent;
            public StateWaitForAction([NotNull] StatePlayerAction parent) : base(parent) => Parent = parent;

            public override EventResult ProcessEvent(RPCEvent e)
            {
                if (e is RPCEventRollDice eRoll)
                {
                    if (eRoll.GamePlayer != Parent.Parent.CurrentPlayer || RollEvent != null || UseItem != null) return EventResult.Invalid;
                    Dice6 dice = eRoll.Dice;
                    eRoll.RollTask = dice.StartRoll(eRoll.GamePlayer);
                }
                else if (e is RPCEventUseItem eUse)
                {
                    if (eUse.GamePlayer != Parent.Parent.CurrentPlayer || RollEvent != null || UseItem != null) return EventResult.Invalid;
                    UseItem = eUse.Item;
                }
                else
                {
                    return EventResult.Invalid;
                }

            }
            public override GameState Update() => null;
            protected override GameState OnClientUpdate(IClientEvent e) => null;
        }

        public class StatePlayerItemEffects : GameState
        {
            public override void Update(ref GameState stateAtomic) { }
        }

        public class StateThrownDice : GameState
        {
            public override void Update(ref GameState stateAtomic) { }
        }

        public class StateThrownItem : GameState
        {
            public override void Update(ref GameState stateAtomic) { }
        }
    }

    public class StateTurnEffects : GameState
    {
        public override void Update(ref GameState stateAtomic) { }
    }

    public class StateNewTurn : GameState
    {
        public override void Update(ref GameState stateAtomic) { }
    }

    public class StateEndTurn : GameState
    {
        public override void Update(ref GameState stateAtomic) { }
    }
}


















public class StateRound : GameState
{
    public readonly int[] RolledDice;

    public StateRound(int[] Roll)
    {
        RolledDice = Roll;
    }

    public override bool ProcessEvent(RPCEvent e)
    {
        if (nextState != null) return false;

        if (e is RPCEventRollDice eRollDice)
        {
            Debug.Log($"Round -- Player: {Game.ActionPlayer.PunConnection.NickName} action -- {eRollDice.GamePlayer.Idx} -- {Game.ActionPlayerIdx}");

            if (eRollDice.GamePlayer.Idx != Game.ActionPlayerIdx) return false;

            if (RolledDice[eRollDice.GamePlayer.Idx] != 0) return false;
            eRollDice.Dice.diceRollCallback = (int result) => {
                RolledDice[eRollDice.GamePlayer.Idx] = result;

                // push a piece move event
                Game.Instance.EventsToProcess.Add(new RPCEventPieceMove()
                {
                    GamePlayer = eRollDice.GamePlayer,
                    Piece = eRollDice.GamePlayer.Piece,
                    MoveStep = result
                });
                eRollDice.Success(result);
            };
            return true;
        }
        else if (e is RPCEventPieceMove ePieceMove)
        {
            // when piece move end
            ePieceMove.Piece.movingCallBack = () => {
                Debug.Log("move end");
                Game.Instance.State = new StateTurnEnd();
            };
            ePieceMove.Piece.photonView.RPC("MoveForward", RpcTarget.AllBufferedViaServer, ePieceMove.MoveStep);
            return true;
        }
        else if (e is RPCEventUseProps eUseProps)
        {
            
        }
        return false;
    }

    public override void Update(ref GameState state)
    {
        //if (RolledDice.All(d => d != 0))
        //{
        //    state = new StateNewRound();
        //    Game.Instance.photonView.RPC("StateChangeNewRound", RpcTarget.AllBufferedViaServer);
        //}
    }
}

public class StateTurnEnd : GameState
{
    public override bool ProcessEvent(RPCEvent e)
    {
        return false;
    }

    public override void Update(ref GameState state)
    {
        if (Game.Instance.roundData.NextPlayer())
        {
            state = RoundData.StateRound;
            Game.Instance.photonView.RPC("StateChangeRound", RpcTarget.AllBufferedViaServer);
        }
        else
        {
            state = new StateNewRound();
            Game.Instance.photonView.RPC("StateChangeNewRound", RpcTarget.AllBufferedViaServer);
        }
    }
}

//public class StatePieceMove : GameState
//{
//    bool isMoveOver = false;
//    public override bool ProcessEvent(RPCEvent e)
//    {
//        if (e is RPCEventPieceMove ePieceMove)
//        {
//            ePieceMove.Piece.photonView.RPC("MoveForward", RpcTarget.AllBufferedViaServer, ePieceMove.MoveStep);
//            return true;
//        }
//        return false;
//    }
//    public override void Update(ref GameState state)
//    {
//        if (isMoveOver)
//        {
//            state = RoundData.StateRound;
//        }
//    }
//}


public class StateUseProps : GameState
{
    public override bool ProcessEvent(RPCEvent e)
    {
        return base.ProcessEvent(e);
    }

    public override void Update(ref GameState stateAtomic)
    {
        throw new NotImplementedException();
    }
}