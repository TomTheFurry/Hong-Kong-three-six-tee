using System;
using System.Collections.Generic;

using Assets.Scripts;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;

public class ItemTemplateDefiner : MonoBehaviourPun
{
    public static ItemTemplateDefiner Instance;
    
    public List<ItemBase> ItemTemplate = new();

    public void Start()
    {
        Instance = this;
        int id = 0;
        foreach (ItemBase child in GetComponentsInChildren<ItemBase>(true))
        {
            child.Id = id++;
            ItemTemplate.Add(child);
            child.gameObject.SetActive(false); // make sure they are not active, for network instantiation
        }
    }

    [PunRPC]
    private void OnInstantiateItem(int typeId, int[] views, PhotonMessageInfo info)
    {
        Debug.Log($"Client-spawning item {typeId}");
        GameObject obj = Instantiate(ItemTemplate[typeId].gameObject);
        Assert.IsFalse(obj.activeSelf);
        ItemBase item = obj.GetComponent<ItemBase>();
        PunNetInstantiateHack.RecieveLinkObj(info.Sender, obj, views);
    }

    public ItemBase ServerInstantiateItem(int typeId)
    {
        Debug.Log($"Spawning item {typeId}");
        GameObject obj = Instantiate(ItemTemplate[typeId].gameObject);
        Assert.IsFalse(obj.activeSelf);
        ItemBase item = obj.GetComponent<ItemBase>();
        PunNetInstantiateHack.SetupForLinkObj(obj, true, (viewIds) =>
        {
            photonView.RPC(nameof(OnInstantiateItem), RpcTarget.OthersBuffered, typeId,  viewIds);
        });
        return item;
    }
}
