using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Dice6 : MonoBehaviourPun
{
    public int DiceValueUp = 1;
    public int DiceValueDown = 6;
    public int DiceValueLeft = 3;
    public int DiceValueRight = 5;
    public int DiceValueFront = 4;
    public int DiceValueBack = 2;

    public bool IsRolling = false;

    private Rigidbody rb;

    private void Awake() { rb = GetComponent<Rigidbody>(); }

    private void Start()
    {
        if (TryGetComponent(out PcGrabInteractable grab))
        {
            grab.GrabCondition = PcGrabCondition;
            grab.OnReleased.AddListener(ClientRollDice);
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
    private const float MAX_BOUNDS = 1000;


    [CanBeNull]
    private GamePlayer PlayerRolling;

    [CanBeNull]
    private volatile TaskCompletionSource<(GamePlayer, int)> OnRollComplete;

    public bool IsStopped() { return idleCount > IDLE_COUNT_THRESHOLD || rb.IsSleeping(); }

    public bool IsInvalid() { return rb.position.magnitude > MAX_BOUNDS; }

    public void Freeze()
    {
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Unfreeze() { rb.isKinematic = false; }

    private void FixedUpdate()
    {
        if (rb.velocity.magnitude < 10f && rb.angularVelocity.magnitude < 10f)
        {
            idleCount++;
        }
        else
        {
            idleCount = 0;
        }

        if (IsRolling && (IsStopped() || IsInvalid()))
        {
            var face = IsInvalid() ? -1 : GetCurrentDiceFace();
            Debug.Log($"Rolled {face}!");
            var obj = OnRollComplete;
            OnRollComplete = null;
            obj.SetResult((PlayerRolling, face));
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

    public bool PcGrabCondition(GamePlayer grabber) { return !IsRolling && PlayerRolling == null; }

    public void ClientRollDice(GamePlayer player)
    {
        if (PlayerRolling != null)
        {
            Debug.LogWarning($"A roll is already in process by {PlayerRolling}, so nope for {player}");
            return;
        }
        OnRollComplete = new TaskCompletionSource<(GamePlayer, int)>();
        PlayerRolling = player;
        IsRolling = true;
        if (PhotonNetwork.IsMasterClient)
        {
            lock (Game.Instance.EventsToProcess)
            {
                Debug.Log($"Player {player} started rolling dice.");
                Game.Instance.EventsToProcess.AddLast(new RPCEventRollDice { Dice = this, GamePlayer = player });
            }
        }
    }

    public async Task<int> WatchForRollDone(GamePlayer expected)
    {
        if (OnRollComplete == null)
        {
            throw new InvalidOperationException("No roll is in progress.");
        }
        var (player, value) = await OnRollComplete.Task;
        if (player != expected)
        {
            throw new InvalidOperationException("Roll was not for the expected player.");
        }
        if (value == -1)
        {
            throw new OperationCanceledException("Roll was cancelled.");
        }
        return value;
    }
}
