using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public class Dice6 : ItemBase
{
    public int DiceValueUp = 1;
    public int DiceValueDown = 6;
    public int DiceValueLeft = 3;
    public int DiceValueRight = 5;
    public int DiceValueFront = 2;
    public int DiceValueBack = 4;

    public bool IsRolling = false;

    public AudioClip HitSound;
    public float HitSoundScale = 0.1f;
    public float HitSoundPitchScale = 1f;

    [NonSerialized]
    private Rigidbody rb;
    private AudioSource audioSource;

    public override bool IsUsable => false;
    protected override bool OnReleasedItem(GamePlayer player)
    {
        if (PlayerRolling != null)
        {
            Debug.LogWarning($"A roll is already in process by {PlayerRolling}, so nope for {player}");
            return true;
        }

        OnRollComplete = new TaskCompletionSource<(GamePlayer, int)>();
        PlayerRolling = player;
        IsRolling = true;

        ApplyRandomTorque();

        if (PhotonNetwork.IsMasterClient)
        {
                Debug.Log($"Player {player} started rolling dice.");
                Game.Instance.PushRPCEvent(new RPCEventRollDice { Dice = this, GamePlayer = player });
        }

        return true;
    }

    private new void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = HitSound;
        if (TryGetComponent(out GrabInteractableBase grab))
        {
            grab.GrabCondition = _ => !IsRolling && PlayerRolling == null;
        }
    }

    // Return current dice face value (the one facing up), or 0 if it's not on a face
    public int GetCurrentDiceFace()
    {
        if (Game.Instance.DEBUG_OverrideDiceRoll != -1) return Game.Instance.DEBUG_OverrideDiceRoll;

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

    public int idleCount = 0;
    private const int IDLE_COUNT_THRESHOLD = 300;
    private const float MAX_BOUNDS = 1000;


    [CanBeNull]
    [NonSerialized]
    public GamePlayer PlayerRolling;

    [CanBeNull]
    private volatile TaskCompletionSource<(GamePlayer, int)> OnRollComplete;

    public bool IsStopped() { return idleCount > IDLE_COUNT_THRESHOLD || rb.IsSleeping(); }

    public bool IsInvalid() { return rb.position.magnitude > MAX_BOUNDS; }

    private void FixedUpdate()
    {
        if (RoomBound.IsOutside(transform))
        {
            transform.position = Vector3.zero + Vector3.up * 2f;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (IsRolling)
            {
                Debug.Log($"Roll cancelled! Dice fall off the world.");
                var obj = OnRollComplete;
                OnRollComplete = null;
                obj.SetResult((PlayerRolling, -1));
                PlayerRolling = null;
                IsRolling = false;
            }
        }
        else
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
                if (face == 0) face = -1;
                Debug.Log($"Rolled {face}!");
                var obj = OnRollComplete;
                OnRollComplete = null;
                obj.SetResult((PlayerRolling, face));
                PlayerRolling = null;
                IsRolling = false;
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
    
    private void ApplyRandomTorque()
    {
        var spherical = UnityEngine.Random.onUnitSphere;
        var torque = new Vector3(spherical.x, spherical.y, spherical.z) * 100;
        rb.AddTorque(torque, ForceMode.VelocityChange);
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

    // when collision is detected, play sound
    private void OnCollisionEnter(Collision collision)
    {
        float magnitude = collision.relativeVelocity.magnitude;
        float withDot = Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal);

        if (withDot > 0.05f)
        {
            float volume = withDot * HitSoundScale;
            volume = Mathf.Clamp(volume, 0, 1);
            float pitch = 1f - HitSoundPitchScale / 2f + volume * HitSoundPitchScale;
            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
