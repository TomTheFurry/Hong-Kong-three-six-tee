using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class Dice6 : MonoBehaviourPun
{
    public int DiceValueUp = 1;
    public int DiceValueDown = 6;
    public int DiceValueLeft = 3;
    public int DiceValueRight = 5;
    public int DiceValueFront = 4;
    public int DiceValueBack = 2;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (TryGetComponent(out PcGrabInteractable grab))
        {
            grab.GrabCondition = PcGrabCondition;
        }
    }

    // Return current dice face value (the one facing up), or 0 if it's not on a face
    public int GetCurrentDiceFace()
    {
        var up = transform.up;
        var right = transform.right;
        var forward = transform.forward;

        var upDot = Vector3.Dot(up, Vector3.up);
        var rightDot = Vector3.Dot(right, Vector3.up);
        var forwardDot = Vector3.Dot(forward, Vector3.up);

        if (upDot > 0.9f)
        {
            return DiceValueUp;
        }
        else if (upDot < -0.9f)
        {
            return DiceValueDown;
        }
        else if (rightDot > 0.9f)
        {
            return DiceValueRight;
        }
        else if (rightDot < -0.9f)
        {
            return DiceValueLeft;
        }
        else if (forwardDot > 0.9f)
        {
            return DiceValueFront;
        }
        else if (forwardDot < -0.9f)
        {
            return DiceValueBack;
        }
        return 0;
    }
    
    private int idleCount = 0;
    private const int IDLE_COUNT_THRESHOLD = 30;
    
    public bool IsStopped()
    {
        return idleCount > IDLE_COUNT_THRESHOLD || rb.IsSleeping();
    }

    public void Freeze()
    {
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Unfreeze()
    {
        rb.isKinematic = false;
    }
    
    private void FixedUpdate()
    {
        if (rb.velocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
        {
            idleCount++;
        }
        else
        {
            idleCount = 0;
        }

        if (IsStopped() && IsRolling)
        {
            var face = GetCurrentDiceFace();
            if (face != 0)
            {
                IsRolling = false;
                if (diceRollCallback != null)
                {
                    diceRollCallback(face);
                    diceRollCallback = null;
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        var up = transform.up;
        var right = transform.right;
        var forward = transform.forward;

        var upDot = Vector3.Dot(up, Vector3.up);
        var rightDot = Vector3.Dot(right, Vector3.up);
        var forwardDot = Vector3.Dot(forward, Vector3.up);

        var color = Color.white;
        if (upDot > 0.9f)
        {
            color = Color.red;
        }
        else if (upDot < -0.9f)
        {
            color = Color.blue;
        }
        else if (rightDot > 0.9f)
        {
            color = Color.green;
        }
        else if (rightDot < -0.9f)
        {
            color = Color.yellow;
        }
        else if (forwardDot > 0.9f)
        {
            color = Color.cyan;
        }
        else if (forwardDot < -0.9f)
        {
            color = Color.magenta;
        }

        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    public GamePlayer PlayerWaitingForRoll { get; private set; }
    public bool IsAwaitingRoll => PlayerWaitingForRoll != null;

    private Action<int> diceRollCallback;

    /// <summary>
    /// Start the roll for the given player. <br/>
    /// ONLY CALL THIS ON THE MASTER CLIENT.
    /// </summary>
    public void StartRoll(GamePlayer player, Action<int> callback)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            throw new InvalidOperationException("Only the master client can start a roll.");
        }
        if (PlayerWaitingForRoll != null)
        {
            throw new InvalidOperationException("A roll is already in progress.");
        }
        if (IsRolling)
        {
            throw new InvalidOperationException("The dice is already rolling.");
        }

        diceRollCallback = callback;
        photonView.RPC(nameof(RpcStartRoll), RpcTarget.All, player.PunConnection);
    }

    /// <summary>
    /// Cancel the roll for the given player. <br/>
    /// ONLY CALL THIS ON THE MASTER CLIENT.
    /// </summary>
    public void CancelRoll()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            throw new InvalidOperationException("Only the master client can cancel a roll.");
        }
        if (!IsAwaitingRoll)
        {
            throw new InvalidOperationException("No roll is in progress.");
        }
        if (diceRollCallback == null)
        {
            throw new InvalidOperationException("No callback was registered for the roll.");
        }
        
        diceRollCallback = null;
        photonView.RPC(nameof(RpcCancelRoll), RpcTarget.All);
    }

    [PunRPC]
    private void RpcStartRoll(Player player)
    {
        PlayerWaitingForRoll = player;
    }

    [PunRPC]
    private void RpcCancelRoll()
    {
        PlayerWaitingForRoll = null;
    }
    
    public bool IsRolling = false;
    
    private void RollByPhysics()
    {
        IsRolling = true;
    }
    
    public bool PcGrabCondition(GamePlayer grabber)
    {
        return !IsRolling && (PlayerWaitingForRoll == null || PlayerWaitingForRoll == grabber);
    }

    public void PcRollDiceOnGrabRelease(GamePlayer grabber)
    {
        if (PlayerWaitingForRoll == grabber)
        {
            RollByPhysics();
        }
    }
}
