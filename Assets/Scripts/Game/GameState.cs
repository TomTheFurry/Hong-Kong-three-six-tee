using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

using Debug = UnityEngine.Debug;

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

    public override string ToString() => $"SwitchState: {StateType}[{ConstructorData.Length} bytes]";
}

public class ClientEventString : IClientEvent
{
    [NotNull]
    public string Key;
    public override string ToString() => $"String: {Key}";
}

public class ClientEventStringData : IClientEvent
{
    [NotNull]
    public string Key;
    [NotNull]
    public byte[] Data;

    public override string ToString() => $"StringData: {Key}[{Data.Length} bytes]";
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
        Debug.Log($"Switching {this}'s child state from {ChildState} to {nextState}");
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
        Debug.Log($"CLIENT: Switching {this}'s child state from {ChildState} to {nextState}");
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
            CanStart = Game.Instance.JoinedPlayers.Count(p => p.Piece != null && p.PlayerObj != null && p.Control != GamePlayer.ControlType.Unknown) >= 1;//2;
            if (MasterSignalStartGame && CanStart)
            {
                Debug.Log("Starting game...");
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

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
                    }
                }

                // Send idx to all players
                Game.Instance.photonView.RPC(nameof(Game.SetIdxToPlayer), RpcTarget.AllBufferedViaServer, Game.Instance.IdxToPlayer.Select(p => p.PunConnection).ToArray() as object);
                // Go to next stage
                for (int i = 0; i < Game.Instance.IdxToPlayer.Length * 2; i++)
                {
                    PhotonNetwork.InstantiateRoomObject("Dice", Game.Instance.DiceSpawnpoint.position, Game.Instance.DiceSpawnpoint.rotation);
                }

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
    private struct RolledDice
    {
        public int Idx;
        public int RollNum;

        public void Deconstruct(out int idx, out int rollNum)
        {
            idx = Idx;
            rollNum = RollNum;
        }
    }

    public readonly int[] RollNumByIdx;
    private LinkedList<RPCEventRollDice> activeRolls = new();

    public StateRollOrder([NotNull] IStateRunner runner) : base(runner)
    {
        RollNumByIdx = new int[Game.Instance.IdxToPlayer.Length];
    }

    public override EventResult ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            if (RollNumByIdx[eRollDice.GamePlayer.Idx] != 0) return EventResult.Invalid;
            Dice6 dice = eRollDice.Dice;
            GamePlayer player = eRollDice.GamePlayer;
            eRollDice.RollTask = dice.WatchForRollDone(player);
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
                    Assert.IsTrue(e.RollTask.IsCompleted);
                    if (e.RollTask.IsCompletedSuccessfully && e.RollTask.Result > 0)
                    {
                        RollNumByIdx[e.GamePlayer.Idx] = e.RollTask.Result;
                        e.Success(e.RollTask.Result);
                        SendClientStateEvent("PlayerRolled", SerializerUtil.Serialize(new RolledDice { Idx = e.GamePlayer.Idx, RollNum = e.RollTask.Result }));
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
        if (RollNumByIdx.All(d => d != 0))
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
            Debug.Log($"Player Order: {playerOrders}");
            SendClientStateEvent("PlayerOrder", SerializerUtil.SerializeArray(playerOrders));
            foreach (var player in Game.Instance.IdxToPlayer)
            {
                player.Piece.photonView.RPC(nameof(Piece.InitCurrentTile), RpcTarget.AllBufferedViaServer);
            }
            // setup game
            Game.Instance.roundData = new RoundData();
            SendClientStateEvent("InitRoundData");


            // Debug code
            ItemTemplateDefiner.Instance.ServerInstantiateItem(18, Game.Instance.IdxToPlayer[0]); // 10, 18

            SendClientSetReturnState<StateTurn>();
            return new StateTurn(Parent, Game.Instance.roundData);
        }
        return null;
    }

    protected override GameState OnClientUpdate(IClientEvent e)
    {
        if (e is ClientEventStringData sd && sd.Key == "PlayerRolled")
        {
            var (idx, roll) = SerializerUtil.Deserialize<RolledDice>(sd.Data);
            GamePlayer player = Game.Instance.IdxToPlayer[idx];
            Debug.Log($"Player {player} Rolled: {roll}");
            RollNumByIdx[idx] = roll;
        }
        else if (e is ClientEventStringData d && d.Key == "PlayerOrder")
        {
            int[] playerOrders = SerializerUtil.DeserializeArray<int>(d.Data);
            Debug.Log($"Player Order: {playerOrders}");
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
    public readonly RoundData Round;
    public readonly int CurrentOrderIdx;
    public int CurrentPlayerIdx => Game.Instance.playerOrder[CurrentOrderIdx];
    [NotNull]
    public GamePlayer CurrentPlayer => Game.Instance.IdxToPlayer[CurrentPlayerIdx];
    
    private Task<bool> _delay;

    public StateTurn([NotNull] IStateRunner parent, [NotNull] RoundData data) : base(parent)
    {
        Round = data;
        Round.CurrentTurnState = this;
        CurrentOrderIdx = Round.ActiveOrderIdx;
        Debug.Log($"Round {Round.RoundIdx}, Turn {CurrentOrderIdx}, Player {CurrentPlayer}: Go!");

        _delay = Round.CurrentPlayer.ProcessPreTurn();
    }

    public override GameState OnSelfUpdate()
    {
        // Start of turn.
        if (!_delay.IsCompleted) return null;
        Assert.IsTrue(_delay.IsCompleted);
        if (_delay.Result)
        {
            // end turn
            SendClientSetReturnState<StateEndTurn>();
            ChildState = new StateEndTurn(this);
        }
        else
        {
            // start turn
            SendClientSetState<StatePlayerAction>();
            ChildState = new StatePlayerAction(this);
        }
        return null;
    }
    
    public override EventResult OnSelfProcessEvent(RPCEvent e) => EventResult.Deferred;
    protected override GameState OnClientEvent(IClientEvent e) => null;

    protected override GameState OnStateReturnControl(GameStateReturn @return)
    {
        if (ChildState is StateEndTurn)
        {
            bool val = (@return as GameStateReturn<bool>)!.Data;
            if (!val)
            {
                // Next turn
                return new StateTurn(Parent, Round);
            }
            else
            {
                // End game
                return new GameStateReturn(Parent);
            }
        }
        return null;
    }

    protected override GameState OnClientStateReturnControl(GameStateReturn @return)
    {
        if (ChildState is StateEndTurn)
        {
            bool val = (@return as GameStateReturn<bool>)!.Data;
            if (!val)
            {
                // Next turn
                return new StateTurn(Parent, Round);
            }
            else
            {
                // End game
                return new GameStateReturn(Parent);
            }
        }
        return null;
    }
    
    protected override GameState ClientCreateState(ClientEventSwitchState s)
    {
        switch (s.StateType)
        {
            case nameof(StatePlayerAction): return new StatePlayerAction(this);
            case nameof(StateEndTurn): return new StateEndTurn(this);
            default: throw new ArgumentOutOfRangeException(s.ToString());
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

        protected override GameState OnClientEvent(IClientEvent e)
        {
            if (e is ClientEventStringData d && d.Key == "RollDice")
            {
                var dice = SerializerUtil.Deserialize<int>(d.Data);
                return new StateTurnEffects(Parent, dice);
            }
            return null;
        }

        protected override GameState OnStateReturnControl(GameStateReturn r)
        {
            if (r is GameStateReturn<int> rInt)
            {
                // Rolled dice.
                return new StateTurnEffects(Parent, rInt.Data);
            }
            if (ChildState is IUseItemState)
            {
                if (r is GameStateReturn<bool> shouldSkipTurn)
                {
                    if (shouldSkipTurn.Data)
                    {
                        SendClientSetReturnState<StateEndTurn>();
                        return new StateEndTurn(Parent);
                    }
                    else
                    {
                        SendClientSetState<StateWaitForAction>();
                        ChildState = new StateWaitForAction(this);
                        return null;
                    }
                }
            }
            return null;
        }

        protected override GameState OnClientStateReturnControl(GameStateReturn @return) => null;

        protected override GameState ClientCreateState(ClientEventSwitchState s)
        {
            switch (s.StateType)
            {
                case nameof(StateWaitForAction):
                    return new StateWaitForAction(this);
                default: throw new ArgumentOutOfRangeException();
            }
        }
        
        public class StateWaitForAction : GameStateLeaf
        {
            [CanBeNull]
            public RPCEventRollDice RollEvent;

            public int RolledDice = 0;
            [CanBeNull]
            public ItemBase UseItem;


            [NotNull]
            public new StatePlayerAction Parent;
            public StateWaitForAction([NotNull] StatePlayerAction parent) : base(parent) => Parent = parent;

            public override EventResult ProcessEvent(RPCEvent e)
            {
                if (e is RPCEventRollDice eRoll)
                {
                    if (eRoll.GamePlayer != Parent.Parent.CurrentPlayer || RollEvent != null || UseItem != null)
                    {
                        return EventResult.Invalid;
                    }
                    Dice6 dice = eRoll.Dice;
                    eRoll.RollTask = dice.WatchForRollDone(eRoll.GamePlayer);
                    RollEvent = eRoll;
                    Debug.Log($"Player {eRoll.GamePlayer} rolling dice now...");
                    return EventResult.Consumed;
                }
                else if (e is RPCEventUseItem eUse)
                {
                    if (eUse.GamePlayer != Parent.Parent.CurrentPlayer || RollEvent != null || UseItem != null || !eUse.Item.IsUsable) return EventResult.Invalid;
                    UseItem = eUse.Item;
                    Debug.Log($"Player {eUse.GamePlayer} using item {UseItem}...");
                    return EventResult.Consumed;
                }
                return EventResult.Invalid;
            }

            public override GameState Update()
            {
                if (RollEvent == null && UseItem == null) return null;

                if (RollEvent != null)
                {
                    if (RollEvent.RollTask.IsCompleted)
                    {
                        Debug.Log($"Roll dice future completed");
                        Assert.IsTrue(RollEvent.RollTask.IsCompleted);
                        if (RollEvent.RollTask.IsCompletedSuccessfully && RollEvent.RollTask.Result > 0)
                        {
                            RollEvent.Success(RollEvent.RollTask.Result);
                            Debug.Log($"Rolled dice {RollEvent.RollTask.Result}");
                            Parent.SendClientStateEvent("RollDice", SerializerUtil.Serialize(RollEvent.RollTask.Result));
                            int steps = RollEvent.RollTask.Result;
                            float multiplier = Parent.Parent.CurrentPlayer.NextTurnRollMultiplier;
                            if (multiplier > 1)
                            {
                                Debug.Log($"Player {Parent.Parent.CurrentPlayer} has next turn roll multiplier {multiplier}");
                                Parent.Parent.CurrentPlayer.NextTurnRollMultiplier = 1;
                                steps = Mathf.CeilToInt(steps * multiplier);
                            }
                            Debug.Log($"Moving {steps} steps");
                            return new GameStateReturn<int>(Parent, steps);
                        }
                        else
                        {
                            Debug.Log($"Dice roll is fail.. Please roll again");
                            RollEvent.Fail();
                            RollEvent = null;
                        }
                    }
                }
                else if (UseItem != null)
                {
                    SendClientStateEvent("UseItem", SerializerUtil.SerializeItem(UseItem));
                    IUseItemState state = UseItem.GetUseItemState(Parent);
                    UseItem.CurrentOwner.RemoveItem(UseItem);
                    return state as GameState;
                }
                return null;
            }

            protected override GameState OnClientUpdate(IClientEvent e)
            {
                if (e is ClientEventStringData d && d.Key == "RollDice")
                {
                    RolledDice = SerializerUtil.Deserialize<int>(d.Data);
                    Debug.Log($"Rolled dice {RolledDice}");
                }
                else if (e is ClientEventStringData d2 && d2.Key == "UseItem")
                {
                    UseItem = SerializerUtil.DeserializeItem(d2.Data);
                    Debug.Log($"Used item {UseItem}");
                    IUseItemState state = UseItem.GetUseItemState(Parent);
                    UseItem.CurrentOwner.RemoveItem(UseItem);
                    return state as GameState;
                }
                return null;
            }
        }
        
    }

    public class StateTurnEffects : NestedGameState
    {
        [NotNull]
        public new StateTurn Parent;

        public readonly int TotalSteps;
        public int Steps;

        public StateTurnEffects([NotNull] StateTurn parent, int steps) : base(parent)
        {
            Parent = parent;
            TotalSteps = Steps = steps;
            Debug.Log($"Turn Effects: Player {Parent.CurrentPlayer} rolled dice {TotalSteps}");
        }
        

        public override GameState OnSelfUpdate()
        {
            // Start of turn.
            SendClientSetState<StateExitTile>();
            ChildState = new StateExitTile(this);
            return null;
        }
    
        public override EventResult OnSelfProcessEvent(RPCEvent e) => EventResult.Deferred;

        protected override GameState OnStateReturnControl(GameStateReturn @return)
        {
            if (ChildState is StateEnterTile)
            {
                // Now, should be at a tile.
                if (Steps > 0)
                {
                    SendClientSetState<StateExitTile>();
                    ChildState = new StateExitTile(this);
                    return null;
                }
                else
                {
                    SendClientSetState<StateStepOnTile>();
                    ChildState = new StateStepOnTile(this);
                    return null;
                }
            }
            else if (ChildState is StateStepOnTile)
            {
                SendClientSetReturnState<StateEndTurn>();
                return new StateEndTurn(Parent);
            }
            return null;
        }


        protected override GameState OnClientStateReturnControl(GameStateReturn @return) => null;
        protected override GameState OnClientEvent(IClientEvent e) => null;

        protected override GameState ClientCreateState(ClientEventSwitchState s)
        {
            switch (s.StateType)
            {
                case nameof(StateExitTile): return new StateExitTile(this);
                case nameof(StateEnterTile): return new StateEnterTile(this);
                case nameof(StateStepOnTile): return new StateStepOnTile(this);
                case nameof(GameStateReturn): return new GameStateReturn(this);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public class StateExitTile : GameStateLeaf
        {
            [NotNull]
            public new StateTurnEffects Parent;
            public StateExitTile([NotNull] StateTurnEffects parent) : base(parent) => Parent = parent;
            public override EventResult ProcessEvent(RPCEvent e) => EventResult.Deferred;

            public override GameState Update()
            {
                GameTile tile = Parent.Parent.Round.ActivePlayerTile;
                if (tile.NeedActionOnExitTile(Parent.Parent.CurrentPlayer))
                {
                    // todo
                }
                else
                {
                    SendClientSetReturnState<StateEnterTile>();
                    return new StateEnterTile(Parent);
                }
                return null;
            }

            protected override GameState OnClientUpdate(IClientEvent e) => null;
        }
        
        public class StateEnterTile : GameStateLeaf
        {
            [NotNull]
            public new StateTurnEffects Parent;
            public Task Animation;
            public bool isPlayerBeingHalted;
            public StateEnterTile([NotNull] StateTurnEffects parent) : base(parent)
            {
                Parent = parent;
                Parent.Steps--;
                var tile = Parent.Parent.Round.ActivePlayerTile.NextTile;
                Debug.Log($"Player {Parent.Parent.CurrentPlayer} enter tile {tile}");
                Animation = Parent.Parent.CurrentPlayer.MoveToTile(tile);
                isPlayerBeingHalted = tile.HaltTurnOnPass;
                if (tile.HaltTurnOnPass)
                {
                    Debug.Log($"Player {Parent.Parent.CurrentPlayer} halt turn on pass tile {tile}");
                    Animation = Animation.ContinueWith(
                        t => Task.Delay(1000), TaskContinuationOptions.ExecuteSynchronously
                    );
                }
            }

            public override EventResult ProcessEvent(RPCEvent e) => EventResult.Deferred;

            public override GameState Update()
            {
                if (!Animation.IsCompleted) return null; // wait for animation

                GameTile tile = Parent.Parent.Round.ActivePlayerTile;
                if (tile.NeedActionOnEnterTile(Parent.Parent.CurrentPlayer))
                {
                    // todo
                }
                else
                {
                    if (isPlayerBeingHalted) Parent.Steps = 0;
                    return new GameStateReturn(Parent);
                }
                return null;
            }

            protected override GameState OnClientUpdate(IClientEvent e) => null;
        }

        public class StateStepOnTile : GameState
        {
            [NotNull]
            public new StateTurnEffects Parent;
            public Task Animation;
            public Task SubStateFuture;
            public GameState Child;

            public StateStepOnTile([NotNull] StateTurnEffects parent) : base(parent)
            {
                Parent = parent;
                GameTile tile = Parent.Parent.Round.ActivePlayerTile;
                Debug.Log($"Player {Parent.Parent.CurrentPlayer} step on tile {tile}");
                tile.ActionsOnStop(Parent.Parent.CurrentPlayer, this, out Animation, out var tmp);
                if (tmp != null)
                {
                    SubStateFuture = tmp.ContinueWith(
                        t =>
                        {
                            Assert.IsTrue(t.IsCompleted);
                            Child = t.Result;
                            Debug.Log($"Switching to state {Child.GetType()}");
                        }, TaskContinuationOptions.ExecuteSynchronously
                    );
                }

                AsyncSkyboxManager.Instance.SwitchToTile(tile);
            }
            public override EventResult ProcessEvent(RPCEvent e) => Child?.ProcessEvent(e) ?? EventResult.Deferred;

            public override GameState Update()
            {
                Debug.Log("Step On Tile Update");
                if (Animation != null)
                {
                    Debug.Log("Chance Step Animation: Is completed = " + Animation.IsCompleted);
                    if (Animation.IsFaulted) throw Animation.Exception;

                    return Animation.IsCompleted ? new GameStateReturn(Parent) : null;
                }
                if (SubStateFuture != null)
                {
                    if (Child == null) return null;
                    SubStateFuture = null;
                }
                Assert.IsNotNull(Child);
                GameState child = Child.Update();
                if (child == null) return null;
                Debug.Log("Chance Step Exit?");
                if (child is GameStateReturn) return new GameStateReturn(Parent);
                throw new Exception("Invalid state");
            }

            public override GameState ClientUpdate(Span<string> tree, IClientEvent e)
            {
                if (Child != null)
                {
                    if (tree.Length == 0) throw new Exception("Invalid tree");
                    if (tree[0] != nameof(StateStepOnTile)) throw new Exception("Invalid state");
                    GameState child = Child.ClientUpdate(tree.Slice(1), e);
                    if (child == null) return null;
                    if (child is GameStateReturn) return new GameStateReturn(Parent);
                    throw new Exception("Invalid state");
                }
                else
                {
                    Debug.LogError("Child state not done yet >.<");
                }
                return null;
            }
        }
    }

    public class StateEndTurn : NestedGameState
    {
        [NotNull]
        public new StateTurn Parent;

        public bool IsEndRound;
        private Task delay;

        public StateEndTurn([NotNull] StateTurn parent) : base(parent)
        {
            Parent = parent;
            var turn = Parent.Round;
            IsEndRound = turn.IsLastPlayer();
            delay = Task.Delay(1000);
            Debug.Log($"Turn end for Player {Parent.CurrentPlayer}");
        }

        public override GameState OnSelfUpdate()
        {
            if (!delay.IsCompleted) return null;

            if (!IsEndRound)
            {
                Parent.Round.NextPlayer();
                SendClientStateEvent("SetOrderIdx", SerializerUtil.Serialize(Parent.Round.ActiveOrderIdx));
                SendClientStateEvent("NextTurn");
                return new GameStateReturn<bool>(Parent, false);
            }
            Debug.Log($"Round {Parent.Round.RoundIdx} end");
            Parent.Round.NextRound();
            SendClientStateEvent("SetRoundIdx", SerializerUtil.Serialize(Parent.Round.RoundIdx));
            SendClientStateEvent("SetOrderIdx", SerializerUtil.Serialize(Parent.Round.ActiveOrderIdx));
            // check for bankrupt
            foreach (var player in Game.Instance.IdxToPlayer)
            {
                if (player.Funds < 0)
                {
                    Debug.Log($"Player {player} is bankrupt. Liquidating its stuff...");
                    SendClientStateEvent("LiquidatePlayer", SerializerUtil.Serialize(player.Idx));
                    player.Liquidate();
                }
            }

            // check for end game
            bool isEndGame = false;
            foreach (var player in Game.Instance.IdxToPlayer)
            {
                if (player.Funds < 0)
                {
                    Debug.Log($"Player {player} is still bankrupt. Ending game.");
                    isEndGame = true;
                    break;
                }
            }
            if (Parent.Round.RoundIdx >= 20)
            {
                Debug.Log($"Ending game after 20 rounds...");
                isEndGame = true;
            }

            if (isEndGame)
            {
                // todo end game screen
                SendClientStateEvent("EndGame");
                return new GameStateReturn<bool>(Parent, true);
            }
            else
            {
                SendClientStateEvent("NextTurn");
                return new GameStateReturn<bool>(Parent, false);
            }
        }


        public override EventResult OnSelfProcessEvent(RPCEvent e) => EventResult.Invalid;

        protected override GameState OnClientEvent(IClientEvent e)
        {
            if (e is ClientEventStringData es)
            {
                switch (es.Key)
                {
                    case "SetOrderIdx":
                        Parent.Round.ActiveOrderIdx = SerializerUtil.Deserialize<int>(es.Data);
                        Debug.Log($"Switched current player to {Parent.Round.CurrentPlayer}");
                        break;
                    case "SetRoundIdx":
                        Parent.Round.RoundIdx = SerializerUtil.Deserialize<int>(es.Data);
                        Debug.Log($"Switched round to {Parent.Round.RoundIdx}");
                        break;
                    case "LiquidatePlayer":
                        Game.Instance.IdxToPlayer[SerializerUtil.Deserialize<int>(es.Data)].Liquidate();
                        break;
                }
            }
            else if (e is ClientEventString es2)
            {
                switch (es2.Key)
                {
                    case "NextTurn":
                        Debug.Log($"Next turn");
                        return new GameStateReturn<bool>(Parent, false);
                    case "EndGame":
                        Debug.Log($"End game");
                        return new GameStateReturn<bool>(Parent, true);
                }
            }
            return null;
        }

        protected override GameState OnStateReturnControl(GameStateReturn @return) => null;
        protected override GameState OnClientStateReturnControl(GameStateReturn @return) => null;
        protected override GameState ClientCreateState(ClientEventSwitchState s) => null;
    }
}