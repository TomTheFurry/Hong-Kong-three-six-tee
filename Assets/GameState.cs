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

public abstract class GameState
{
    protected GameState nextState = null;
    protected RPCEvent nextEvent = null;
    public virtual bool ProcessEvent(RPCEvent e)
    {
        return false;
    }
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
                    piece.photonView.RPC("UpdataOwnerMaterial", RpcTarget.AllBufferedViaServer);
                    piece.photonView.RPC("InitCurrentTile", RpcTarget.AllBufferedViaServer);
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
    public readonly int[][] RolledDice;

    public StateChooseOrder()
    {
        RolledDice = new int[Game.Instance.IdxToPlayer.Length][];
        for (int i = 0; i < RolledDice.Length; i++)
        {
            RolledDice[i] = new int[] { i, 0 };
        }
    }

    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            if (RolledDice[eRollDice.GamePlayer.Idx][1] != 0) return false;
            eRollDice.Dice.diceRollCallback = (int result) => {
                if (RolledDice.Any(d => d[1] == result)) return;
                RolledDice[eRollDice.GamePlayer.Idx][1] = result;
                eRollDice.Success(result);
            };
            return true;
        }
        return false;
    }

    public override void Update(ref GameState state)
    {
        if (RolledDice.All(d => d[1] != 0))
        {
            int i = 0;
            foreach (int[] order in RolledDice.OrderBy(d => d[1]))
            {
                Game.Instance.playerOrder[i++] = order[0];
            }

            state = new StateNewRound();
            Game.Instance.photonView.RPC("StateChangeNewRound", RpcTarget.AllBufferedViaServer);
        }
    }
}

public class StateNewRound : GameState
{
    public StateNewRound()
    {
        Game.Instance.round = new RoundData(Game.Instance);
    }

    public override void Update(ref GameState state)
    {
        state = Game.Instance.round.stateRound;
        Game.Instance.photonView.RPC("StateChangeRound", RpcTarget.AllBufferedViaServer);
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
            nextState = new StateRolledDice();
            nextEvent = eRollDice;

            return true;
        }
        return false;
    }

    public override void Update(ref GameState state)
    {
        if (nextState != null)
        {
            state = nextState;
            Game.Instance.EventsToProcess.Add(nextEvent);
        }
        else if (RolledDice.All(d => d != 0))
        {
            state = new StateNewRound();
            Game.Instance.photonView.RPC("StateChangeNewRound", RpcTarget.AllBufferedViaServer);
        }
    }
}

public class StateRolledDice : GameState
{
    public readonly int[] RolledDice;

    public StateRolledDice()
    {
        RolledDice = Game.Instance.round.RolledDice;
    }

    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventRollDice eRollDice)
        {
            int idx = eRollDice.GamePlayer.Idx;
            if (RolledDice[idx] != 0) return false;
            RolledDice[idx] = Random.Range(1, 7);
            eRollDice.Success(RolledDice[idx]);
        }
        return base.ProcessEvent(e);
    }
    public override void Update(ref GameState state)
    {
        state = Game.Instance.round.stateRound;
    }
}

public class StatePieceMove : GameState
{
    bool isMoveOver = false;
    public override bool ProcessEvent(RPCEvent e)
    {
        if (e is RPCEventPieceMove ePieceMove)
        {
            ePieceMove.Piece.photonView.RPC("MoveForward", RpcTarget.AllBufferedViaServer, ePieceMove.MoveStep);
            return true;
        }
        return false;
    }
    public override void Update(ref GameState state)
    {
        if (isMoveOver)
        {
            state = Game.Instance.round.stateRound;
        }
    }
}


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