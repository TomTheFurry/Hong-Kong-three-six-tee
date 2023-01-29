using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using Unity.VisualScripting;

using UnityEngine;
using TMPro;

using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpringJoint))]
[RequireComponent(typeof(PhotonView))]
public class Piece : MonoBehaviourPun, IOnPhotonViewOwnerChange
{
    private GamePlayer _owner;
    public GamePlayer Owner
    {
        get => _owner;
        set
        {
            _owner = value;
        }
    }

    public bool ControlOverrideByServer { get; private set; } = true;

    public Player TargetController => ControlOverrideByServer ? PhotonNetwork.MasterClient : (Owner?.PunConnection ?? PhotonNetwork.MasterClient);
    public bool IsOwnershipStateValid => photonView.Owner == TargetController;

    public GameTile CurrentTile = null;
    public TMP_Text Nametag = null;

    private Rigidbody rb;
    private SpringJoint sj;
    
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        sj = GetComponent<SpringJoint>();
        sj.autoConfigureConnectedAnchor = false;
        Teleport();
        UpdatePin();
    }

    private float t = 0;
    public void Update()
    {
        if (Time.timeSinceLevelLoad - t > 0)
        {
            t+= Random.Range(0.5f, 2f);
            MoveForward(1);
            //Teleport();
        }

        // update nametag
        if (Owner == null)
        {
            Nametag.gameObject.SetActive(false);
        }
        else
        {
            Nametag.gameObject.SetActive(true);
            Nametag.text = Owner.PunConnection.NickName;

            // make nametag face camera
            Nametag.transform.LookAt(Camera.main.transform); // LookAt() doesn't correct for roll
            Nametag.transform.Rotate(0, 180, 0, Space.Self);

        }
    }

    // Called by server side
    public void SetControlOverride(bool controlOverride)
    {
        Debug.Assert(PhotonNetwork.IsMasterClient);
        photonView.RPC("UpdateControlState", RpcTarget.AllBufferedViaServer, Owner?.PunConnection, controlOverride);
    }

    // Called by server side
    public void SetOwner(GamePlayer owner)
    {
        Debug.Assert(PhotonNetwork.IsMasterClient);
        photonView.RPC("UpdateControlState", RpcTarget.AllBufferedViaServer, owner?.PunConnection, ControlOverrideByServer);
    }

    public void Set(GamePlayer owner, bool controlOverride)
    {
        Debug.Assert(PhotonNetwork.IsMasterClient);
        photonView.RPC("UpdateControlState", RpcTarget.AllBufferedViaServer, owner?.PunConnection, controlOverride);
    }

    [PunRPC]
    public void UpdateControlState([CanBeNull] Player owner, bool controlOverride, PhotonMessageInfo info)
    {
        if (info.Sender != PhotonNetwork.MasterClient)
        {
            Debug.LogError($"Invalid RPC call from non-master client to update control state by {info.Sender}");
            return;
        }
        Debug.Log($"Update control state to owner: {owner}, with controlOverride: {controlOverride}");
        Owner = owner;
        ControlOverrideByServer = controlOverride;
        if (photonView.IsMine && !IsOwnershipStateValid)
        {
            photonView.TransferOwnership(TargetController);
        }
    }

    public void Teleport()
    {
        if (CurrentTile == null) return;
        rb.position = CurrentTile.transform.position + Vector3.up * 1f;
        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
        rb.rotation = Quaternion.AngleAxis(Random.value * Mathf.PI, Vector3.up);
    }
    

    private void UpdatePin()
    {
        sj.connectedAnchor = CurrentTile.transform.position;
    }

    public void MoveForward(int number)
    {
        while (number-- != 0)
        {
            CurrentTile = CurrentTile.NextTile;
        }
        UpdatePin();
    }
    
    public void OnOwnerChange(Player newOwner, Player previousOwner) { }
}
