using System.Collections.Generic;

using Assets.Scripts;

using Photon.Pun;

using UnityEditor;

using UnityEngine;
using UnityEngine.Assertions;

public abstract class UseItemStateBase : NestedGameState
{
    public new readonly StateTurn.StatePlayerAction Parent;
    public readonly ItemBase Item;
    public readonly GamePlayer Player;
    public readonly RoundData Round;

    protected UseItemStateBase(StateTurn.StatePlayerAction parent, ItemBase item) : base(parent)
    {
        Parent = parent;
        Item = item;
        Player = parent.Parent.CurrentPlayer;
        Round = parent.Parent.Round;
    }
}

public class ItemTemplateDefiner : MonoBehaviourPun
{
    public static ItemTemplateDefiner Instance;

    public List<ItemBase> ItemTemplate = new();

    public void Start()
    {
        Instance = this;
        int id = 0;
        foreach (ItemBase child in transform.GetComponentsInChildren<ItemBase>())
        {
            child.Id = id++;
            ItemTemplate.Add(child);
            child.gameObject.SetActive(false); // make sure they are not active, for network instantiation
        }
    }

    private void OnInstantiateItem(int typeId, int instanceId, int[] views, PhotonMessageInfo info)
    {
        GameObject obj = Instantiate(ItemTemplate[typeId].gameObject);
        Assert.IsFalse(obj.activeSelf);
        ItemBase item = obj.GetComponent<ItemBase>();
        PunNetInstantiateHack.RecieveLinkObj(info.Sender, obj, views);
    }

    public ItemBase ServerInstantiateItem(int typeId)
    {
        GameObject obj = Instantiate(ItemTemplate[typeId].gameObject);
        Assert.IsFalse(obj.activeSelf);
        ItemBase item = obj.GetComponent<ItemBase>();
        int instanceId = UnityEngine.Random.Range(0, int.MaxValue);
        PunNetInstantiateHack.SetupForLinkObj(obj, true, (viewIds) =>
        {
            photonView.RPC(nameof(OnInstantiateItem), RpcTarget.OthersBuffered, typeId, instanceId, viewIds);
        });
        return item;
    }
}

public abstract class ItemBase : PcGrabInteractable
{
    private GamePlayer CurrentOwnerImpl = null;

    public GamePlayer CurrentOwner
    {
        get => CurrentOwnerImpl;
        set
        {
            if (CurrentOwnerImpl == value)
            {
                return;
            }

            if (CurrentOwnerImpl != null)
            {
                CurrentOwnerImpl.Items.Remove(this);
            }
            if (value != null)
            {
                value.Items.AddFirst(this);
            }
            CurrentOwnerImpl = value;
        }
    }



    public int Id { get; set; }
    public int InstanceId => photonView.InstantiationId;
    public abstract bool IsUsable { get; }
    public abstract UseItemStateBase GetUseItemState(StateTurn.StatePlayerAction parent);

    protected ItemBase()
    {
        GrabCondition = (player) => CurrentOwner == player;
        OnReleased.AddListener((player) =>
            {
                bool releaseControl = OnReleasedItem(player);
                if (releaseControl)
                {
                    CurrentOwner = null;
                }
            }
        );
    }

    protected abstract bool OnReleasedItem(GamePlayer player);

}

/*
public class PlaceOnTileItem : ItemBase
{
    public bool IsUsable => true;

    public override UseItemStateBase GetUseItemState()
    {
        //todo
    }
}
*/

