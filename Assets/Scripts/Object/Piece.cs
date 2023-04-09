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
using System.Threading.Tasks;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpringJoint))]
[RequireComponent(typeof(PhotonView))]
public class Piece : MonoBehaviourPun, IOnPhotonViewOwnerChange
{
    public GamePlayer Owner;

    public bool ControlOverrideByServer { get; private set; } = true;

    public Player TargetController => ControlOverrideByServer ? PhotonNetwork.MasterClient : (Owner?.PunConnection ?? PhotonNetwork.MasterClient);
    public bool IsOwnershipStateValid => photonView.Owner == TargetController;

    public GameTile CurrentTile = null;
    public TMP_Text Nametag = null;

    private Rigidbody rb;
    private SpringJoint sj;
    
    private bool moving = false;
    /// <summary>
    /// Will be called when piece move end
    /// </summary>
    public Action movingCallBack;
    
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
        // *** not used
        if (false && Time.timeSinceLevelLoad - t > 0)
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

            Vector2 tilePos = new Vector2(CurrentTile.transform.position.x, CurrentTile.transform.position.z);
            Vector2 piecePos = new Vector2(transform.position.x, transform.position.z);
            if (moving)
            {
                Debug.Log(Vector2.Distance(tilePos, piecePos));
                GetComponent<Rigidbody>().WakeUp();
            }
            if (moving && Vector2.Distance(tilePos, piecePos) < 0.1f)
            {
                movingCallBack?.Invoke();
                movingCallBack = null;
                moving = false;
            }
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

    [PunRPC]
    public void UpdataOwnerMaterial()
    {
        Material mat = GetComponent<Renderer>().material;
        foreach (Transform transform in Owner?.PlayerObj.transform)
        {
            if (transform.name.Equals("Capsule"))
            {
                transform.GetComponent<Renderer>().material = mat;
            }
        }
    }

    [PunRPC]
    public void InitCurrentTile() {
        CurrentTile = Board.StartTile;
        Teleport();
    }

    [PunRPC]
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

    public Task MoveToTile(GameTile tile)
    {
        CurrentTile = tile;
        var t = new TaskCompletionSource<int>();
        moving = false;
        movingCallBack = () => t.SetResult(0);
        moving = true;
        UpdatePin();
        return Task.WhenAny(t.Task, Task.Delay(2000))
            .ContinueWith((t) => { moving = false; });
    }

    [PunRPC]
    public void MoveForward(int number)
    {
        while (number-- != 0)
        {
            CurrentTile = CurrentTile.NextTile;
        }
        moving = true;
        UpdatePin();
    }
    
    public void OnOwnerChange(Player newOwner, Player previousOwner) { }
}
