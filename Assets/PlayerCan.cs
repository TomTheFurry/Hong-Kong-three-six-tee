using System.Collections;
using System.Collections.Generic;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class PlayerCan : PcGrabInteractable, IPunInstantiateMagicCallback
{
    public GamePlayer CanOwner;

    public PlayerCan() {
        GrabCondition = (player) => CanOwner == player;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        CanOwner = info.photonView.InstantiationData[0] as Player;
        Assert.IsNull(CanOwner.Can);
        CanOwner.Can = this;
    }

    public static PlayerCan Instantiate(GamePlayer owner)
    {
        Assert.IsTrue(PhotonNetwork.IsMasterClient);
        return PhotonNetwork.Instantiate("PlayerCan", Vector3.zero, Quaternion.identity, 0, new object[] { owner.PunConnection }).GetComponent<PlayerCan>();
    }

    public void SlotInItem(ItemBase item)
    {
        Assert.IsTrue(item.CurrentOwner == CanOwner);

        item.transform.SetParent(transform);
        void OnItemGrabbed(GamePlayer player)
        {
            item.OnGrabbed.RemoveListener(OnItemGrabbed);
            item.transform.SetParent(null);
        }
        item.OnGrabbed.AddListener(OnItemGrabbed);
    }
}
