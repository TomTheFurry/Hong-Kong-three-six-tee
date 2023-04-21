using System.Collections.Generic;

using Assets.Scripts;

using JetBrains.Annotations;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;
using UnityEngine.Assertions;

public class ItemTemplateDefiner : MonoBehaviourPun
{
    public static ItemTemplateDefiner Instance;

    public Transform ItemSpawnpoint;
    
    public List<GameItem> ItemTemplate = new();
    public List<GameItem> ShopItems = new();

    public void Start()
    {
        Instance = this;
        int id = 0;
        foreach (GameItem child in GetComponentsInChildren<GameItem>(true))
        {
            child.Id = id++;
            ItemTemplate.Add(child);
            child.gameObject.SetActive(false); // make sure they are not active, for network instantiation
            if (child.CanBuyInShop) {
                ShopItems.Add(child);
            }
        }
    }

    [PunRPC]
    public void OnInstantiateItem(int typeId, int[] views, [CanBeNull] Player player, PhotonMessageInfo info)
    {
        Debug.Log($"Client-spawning item {typeId}");
        GameItem item = Instantiate(ItemTemplate[typeId].gameObject).GetComponent<GameItem>();
        item.transform.position = ItemSpawnpoint.position;
        item.transform.rotation = ItemSpawnpoint.rotation;
        item.CurrentOwner = player;
        PunNetInstantiateHack.RecieveLinkObj(info.Sender, item.gameObject, views);
    }

    public GameItem ServerInstantiateItem(int typeId, GamePlayer owner = null)
    {
        Debug.Log($"Spawning item {typeId}");
        GameItem item = Instantiate(ItemTemplate[typeId].gameObject).GetComponent<GameItem>();
        item.transform.position = ItemSpawnpoint.position;
        item.transform.rotation = ItemSpawnpoint.rotation;
        item.CurrentOwner = owner;
        PunNetInstantiateHack.SetupForLinkObj(item.gameObject, true, (viewIds) =>
        {
            photonView.RPC(nameof(OnInstantiateItem), RpcTarget.OthersBuffered, typeId,  viewIds, owner?.PunConnection);
        });
        return item;
    }

    public int[] ServerDrawShopItems() {
        GameItem[] items = new GameItem[3];
        for (int i = 0; i < 3; i++) {
            int randomIndex = Random.Range(0, ShopItems.Count);
            items[i] = ShopItems[randomIndex];
        }
        return new int[] { items[0].Id, items[1].Id, items[2].Id };
    }
}
