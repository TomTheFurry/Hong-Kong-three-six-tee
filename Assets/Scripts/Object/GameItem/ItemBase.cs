using System;

using UnityEngine;

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

public abstract class ItemBase : PcGrabInteractable
{
    private GamePlayer CurrentOwnerImpl = null;
    [NonSerialized]
    public Rigidbody rb;

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
    public bool IsIllegal = false;
    public bool IsHidden;

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

    protected virtual bool OnReleasedItem(GamePlayer player)
    {
        PlayerCan can = player.Can;
        if (can != null && can.Collider.bounds.Contains(transform.position))
        {
            can.SlotInItem(this);
        }
        return false;
    }

    public void Start() {
        rb = GetComponent<Rigidbody>();
    }

    public new void FixedUpdate()
    {
        if (RoomBound.IsOutside(transform))
        {
            transform.position = Vector3.zero + Vector3.up * 2f;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        base.FixedUpdate();
    }

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

