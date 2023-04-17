using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assets.Scripts;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

using Random = UnityEngine.Random;

public class ChanceSpawner : MonoBehaviourPun
{
    public static ChanceSpawner Instance;

    public List<ChanceEvent> Events0;
    public List<ChanceEvent> Events1;
    public List<ChanceEvent> Events2;
    public List<ChanceEvent> Events3;
    public ChanceCard Template;
    public List<GameItem> ItemLinks;

    [NonSerialized]
    public List<ChanceEvent>[] EventsGroups = new List<ChanceEvent>[4];
    
    [NonSerialized]
    public List<ChanceEvent> Events;

    private TaskCompletionSource<ChanceCard> SpawnCardTcs;
    private GamePlayer AwaitingPlayer;
    private int DrawGroup;

    public void Start()
    {
        Instance = this;
        EventsGroups[0] = Events0;
        EventsGroups[1] = Events1;
        EventsGroups[2] = Events2;
        EventsGroups[3] = Events3;
        int id = 0;
        foreach (var group in EventsGroups)
        {
            foreach (var e in group)
            {
                e.Id = id++;
            }
        }
        Events = EventsGroups.SelectMany(g => g).ToList();
    }

    private ChanceEvent DrawEventWeighted(int group, float luck)
    {
        var events = EventsGroups[group];

        var totalWeight = events.Sum(e => e.ChanceMuliplier > 0 ? luck * e.ChanceMuliplier : 1);
        var rand = Random.Range(0, totalWeight);
        float sum = 0;
        foreach (var e in events)
        {
            sum += e.ChanceMuliplier > 0 ? luck * e.ChanceMuliplier : 1;
            if (rand <= sum)
            {
                return e;
            }
        }
        throw new System.Exception("Should not reach here");
    }

    [PunRPC]
    public void SpawnCard(int cardId, int[] views)
    {
        Assert.IsNotNull(SpawnCardTcs);
        var card = Instantiate(Template, transform.position + Vector3.up * 1f, Quaternion.identity);
        if (card.gameObject.activeSelf)
        {
            Debug.LogError("Card prefab should be set to inactive!");
            card.gameObject.SetActive(false);
        }
        card.Ev = Events[cardId];
        card.Description.text = Events[cardId].Description;
        card.Title.text = Events[cardId].Name;
        PunNetInstantiateHack.RecieveLinkObj(PhotonNetwork.LocalPlayer, card.gameObject, views);
        card.gameObject.SetActive(true);
        SpawnCardTcs.SetResult(card);
        SpawnCardTcs = null;
        AwaitingPlayer = null;
    }

    public void ClientWantDrawCard()
    {
        GamePlayer p = PhotonNetwork.LocalPlayer;
        if (p != AwaitingPlayer)
        {
            Debug.Log("Ignored client draw card attempt");
            return;
        }
        photonView.RPC(nameof(ServerDrawCard), RpcTarget.MasterClient);
    }

    [PunRPC]
    public void ServerDrawCard(PhotonMessageInfo info)
    {
        Debug.Log("Drawing cards...");
        if (info.Sender != AwaitingPlayer.PunConnection)
        {
            Debug.Log("Ignored draw card attempt");
            return;
        }

        var card = Instantiate(Template, transform.position + Vector3.up * 1f, Quaternion.identity);
        if (card.gameObject.activeSelf)
        {
            Debug.LogError("Card prefab should be set to inactive!");
            card.gameObject.SetActive(false);
        }
        var e = DrawEventWeighted(DrawGroup, AwaitingPlayer.Luck);
        Debug.Log($"Drawn event: {e.Name}");
        card.Ev = e;
        card.Description.text = e.Description;
        card.Title.text = e.Name;
        PunNetInstantiateHack.SetupForLinkObj(card.gameObject, true, (viewIds) =>
        {
            photonView.RPC(nameof(SpawnCard), RpcTarget.OthersBuffered, e.Id, viewIds);
        });
        card.gameObject.SetActive(true);
        SpawnCardTcs.SetResult(card);
    }

    public Task<ChanceCard> AwaitForDrawCard(int chanceGroup, GamePlayer player)
    {
        Assert.IsNull(SpawnCardTcs);
        SpawnCardTcs = new TaskCompletionSource<ChanceCard>();
        DrawGroup = chanceGroup;
        AwaitingPlayer = player;
        return SpawnCardTcs.Task;
    }


    // temp!

    public InputActionReference DrawCardAction;

    public void Update()
    {
        if (DrawCardAction.action.triggered)
        {
            ClientWantDrawCard();
        }
    }

}
