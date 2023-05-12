using System;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(PhotonView))]
public class GrabInteractableBase : MonoBehaviourPun
{
    public int ownerID = -1;
    public bool IsGrabbed => ownerID != -1;
    public bool IsGrabbedByMe => IsGrabbed && ownerID == PhotonNetwork.LocalPlayer.ActorNumber;

    public Transform CurrentLocalGrabber = null;
    public Action AsyncCallback = null;
    public Transform TriedGrabber = null;

    public float rotationP = 0.4f;
    public float rotationD = 0.4f;
    public float positionP = 8f;
    public float positionD = 0.5f;
    public float attachYOffset = 0.15f;
    public float changeYSpeed = 0.01f;

    public bool noGravityAfterGrab = true;

    [NotNull]
    public UnityEvent<GamePlayer> OnGrabbed = new();

    [NotNull]
    public UnityEvent<GamePlayer> OnReleased = new();

    [NotNull]
    public Func<GamePlayer, bool> GrabCondition = _ => true;

    public bool canGrab => !IsGrabbed && GrabCondition(PhotonNetwork.LocalPlayer);
    public bool canRelease => IsGrabbedByMe;

    public void Awake()
    {
        if (XrManager.HasXRDevices) {
            gameObject.AddComponent<XrGrab>();
        }
        else {
            gameObject.AddComponent<PcGrab>();
        }
    }


    public void TryGrabObject(Transform grabber, Action onSuccess)
    {
        if (IsGrabbedByMe)
        {
            Debug.LogError("Already grabbed by me");
            return;
        }

        GamePlayer grabPlayer = PhotonNetwork.LocalPlayer;
        Debug.Log($"Player {grabPlayer} try grab {gameObject}...");
        if (PhotonNetwork.InLobby || !PhotonNetwork.IsConnected)
        {
            // Skip sync stuff
            CurrentLocalGrabber = grabber;
            grabPlayer.Holding.Add(gameObject);
            OnGrabbed.Invoke(grabPlayer);
            onSuccess();

        }
        else if (IsGrabbed || !GrabCondition(grabPlayer))
        {
            // Return
        }
        else
        {
            AsyncCallback = onSuccess;
            TriedGrabber = grabber;
            photonView.RPC("RequestGrabObject", photonView.Controller);
        }
    }

    [PunRPC]
    public void NotifyChangeOwner([NotNull] Player player, bool isReleasing)
    {
        if (isReleasing)
        {
            ((GamePlayer)player).Holding.Remove(gameObject);
            OnReleased.Invoke(player);
            ownerID = -1;
            return;
        }
        ownerID = player.ActorNumber;
        ((GamePlayer)player).Holding.Add(gameObject);

        OnGrabbed.Invoke(player);

        if (player.IsLocal)
        {
            if (TriedGrabber != null)
            {
                Debug.Log("Grab successful");
                CurrentLocalGrabber = TriedGrabber;
                AsyncCallback?.Invoke();
                AsyncCallback = null;
                TriedGrabber = null;
            }
            else
            {
                Debug.Log("Unknown grab received");
                if (photonView.IsMine)
                {
                    photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    photonView.RPC("NotifyChangeOwner", RpcTarget.OthersBuffered, player, true);
                }
            }
        }
        else
            CurrentLocalGrabber = null;
    }

    [PunRPC]
    public void NotifyFailedGrab(string message) { Debug.Log("Grab failed: " + message); }

    [PunRPC]
    public void RequestGrabObject(PhotonMessageInfo info)
    {
        Debug.Log("Grab request received by " + info.Sender.ActorNumber);
        if (IsGrabbed)
        {
            Debug.Log("Object is already grabbed");
            photonView.RPC("NotifyFailedGrab", info.Sender, "Object is already grabbed");
            return;
        }
        if (!GrabCondition(info.Sender))
        {
            Debug.Log("Grab condition failed");
            photonView.RPC("NotifyFailedGrab", info.Sender, "Grab condition failed");
            return;
        }
        photonView.TransferOwnership(info.Sender);
        photonView.RPC("NotifyChangeOwner", RpcTarget.AllBuffered, info.Sender, false);
    }

    public void ReleaseObject()
    {
        if (!photonView.IsMine)
        {
            Debug.LogError("Object is not grabbed by this player");
            return;
        }
        var temp = CurrentLocalGrabber;
        CurrentLocalGrabber = null;
        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("Release object noted, and no sync needed because we are in lobby");
            GamePlayer player = PhotonNetwork.LocalPlayer;
            OnReleased.Invoke(player);
            return;
        }
        photonView.TransferOwnership(PhotonNetwork.MasterClient);
        photonView.RPC("NotifyChangeOwner", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer, true);
    }
    
    private void OnDestroy()
    {
        if (IsGrabbed)
        {
            GamePlayer player = PhotonNetwork.CurrentRoom.GetPlayer(ownerID, true);
            if (player != null)
            {
                player.Holding.Remove(gameObject);
                OnReleased.Invoke(player);
            }
        }
    }

}