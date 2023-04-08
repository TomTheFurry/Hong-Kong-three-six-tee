using Photon.Pun;
using UnityEngine;
using System.Linq;

public class OwnedObject : MonoBehaviourPun
{
    public bool HidenByOwners { get; private set; }
    private GameObject child;

    public void Start() { child = transform.GetChild(0).gameObject; }

    void Update()
    {
        if (HidenByOwners && !photonView.IsMine)
        {
            child.GetComponents<Rigidbody>().Select(b => b.isKinematic = false);
            child.GetComponents<PhotonRigidbodyView>().Select(b => b.enabled = false);
            child.GetComponents<Collider>().Select(c => c.enabled = false);
            child.GetComponents<Renderer>().Select(r => r.enabled = false);
        }
        else
        {
            child.GetComponents<Rigidbody>().Select(b => b.isKinematic = true);
            child.GetComponents<PhotonRigidbodyView>().Select(b => b.enabled = true);
            child.GetComponents<Collider>().Select(c => c.enabled = true);
            child.GetComponents<Renderer>().Select(r => r.enabled = true);
        }
    }

    public void RPCRevealObject()
    {
        photonView.RPC(nameof(RevealOrHideObject), RpcTarget.AllBuffered, false);
    }
    public void RPCHideObject()
    {
        photonView.RPC(nameof(RevealOrHideObject), RpcTarget.AllBuffered, true);
    }

    [PunRPC]
    private void RevealOrHideObject(bool hidden)
    {
        Debug.Assert(PhotonNetwork.IsMasterClient, "Must be the master client to control whether object is hidden.");
        HidenByOwners = hidden;
    }

}
