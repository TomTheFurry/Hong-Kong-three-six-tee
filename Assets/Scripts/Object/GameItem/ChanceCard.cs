using System;

using JetBrains.Annotations;

using Photon.Pun;

using TMPro;

using UnityEngine;

using Object = UnityEngine.Object;

public class ChanceCard : GrabInteractableBase, IPunInstantiateMagicCallback
{
    public ChanceEvent Ev;
    public TextMeshPro Title;
    public TextMeshPro Description;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Ev = ChanceSpawner.Instance.Events[(int)info.photonView.InstantiationData[0]];
        Title.text = Ev.Name;
        Description.text = Ev.Description;
    }

    [PunRPC]
    public void Remove()
    {
        photonView.ViewID = 0;
        Object.Destroy(gameObject);
    }
}

public abstract class ChanceEvent : ScriptableObject
{
    [NonSerialized]
    public int Id;

    public string Name;
    public string Description;

    public int ChanceMuliplier = 0;
    public abstract void OnCreate(ChanceEventState r);
    public virtual bool ServerUpdate(ChanceEventState r) => true;
    public virtual EventResult OnServerEvent(ChanceEventState r, RPCEvent e) => EventResult.Deferred;
    public virtual bool OnClientEvent(ChanceEventState r, IClientEvent e) => throw new System.NotImplementedException();
}

public class ChanceEventState : GameStateLeaf
{
    public GamePlayer Player => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.CurrentPlayer;
    public RoundData Data => (Parent as StateTurn.StateTurnEffects.StateStepOnTile).Parent.Parent.Round;
    private readonly ChanceEvent Ev;
    public readonly ChanceCard Card;
    public ChanceEventState([NotNull] StateTurn.StateTurnEffects.StateStepOnTile parent, ChanceCard card) : base(parent)
    {
        Card = card;
        Ev = card.Ev;
        Ev.OnCreate(this);
        Debug.Log("Done state creation");
    }

    public override EventResult ProcessEvent(RPCEvent e) => Ev.OnServerEvent(this, e);

    public override GameState Update()
    {
        if (Ev.ServerUpdate(this))
        {
            Debug.Log("Exiting chance event state...");
            Card.photonView.RPC(nameof(Card.Remove), RpcTarget.Others);
            Card.photonView.ViewID = 0;
            Object.Destroy(Card.gameObject);
            return new GameStateReturn(Parent);
        }
        return null;
    }

    protected override GameState OnClientUpdate(IClientEvent e) => Ev.OnClientEvent(this, e) ? new GameStateReturn(Parent) : null;
}