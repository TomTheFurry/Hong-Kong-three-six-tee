using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(BoxCollider))]
public class PlayerCan : GrabInteractableBase, IPunInstantiateMagicCallback
{
    public GamePlayer CanOwner;
    public BoxCollider Collider;

    public PlayerCan() {
        GrabCondition = (player) => CanOwner == player;
    }

    void Start()
    {
        Collider = GetComponent<BoxCollider>();
        Collider.isTrigger = true;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        CanOwner = info.photonView.InstantiationData[0] as Player;
        Assert.IsNotNull(CanOwner);
        Assert.IsNull(CanOwner.Can);
        CanOwner.Can = this;
    }

    public static PlayerCan Instantiate(GamePlayer owner)
    {
        Assert.IsTrue(PhotonNetwork.IsMasterClient);
        var sp = Game.Instance.GetSpawnpoint(owner.Idx);
        return PhotonNetwork.Instantiate("PlayerCan", sp.position, sp.rotation, 0, new object[] { owner.PunConnection }).GetComponent<PlayerCan>();
    }

    public void SlotInItem(ItemBase item)
    {
        Assert.IsTrue(item.CurrentOwner == CanOwner);

        item.transform.SetParent(transform);
        item.rb.isKinematic = true;
        item.rb.useGravity = false;
        item.rb.velocity = Vector3.zero;
        item.rb.angularVelocity = Vector3.zero;

        void OnItemGrabbed(GamePlayer player)
        {
            item.OnGrabbed.RemoveListener(OnItemGrabbed);
            item.transform.SetParent(null);
            item.rb.isKinematic = false; 
            item.rb.useGravity = true;
        }
        item.OnGrabbed.AddListener(OnItemGrabbed);
    }
}
