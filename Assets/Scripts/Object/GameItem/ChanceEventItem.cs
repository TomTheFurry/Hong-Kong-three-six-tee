using System.Linq;

using JetBrains.Annotations;

using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventItem", menuName = "ChanceEvent/Item")]
public class ChanceEventItem : ChanceEvent {
    public int ItemToGainIdx;

    public override void OnCreate(ChanceEventState r)
    {
        if (!r.IsMaster) return;

        if (ItemToGainIdx != -1)
        {
            ItemTemplateDefiner.Instance.ServerInstantiateItem(ChanceSpawner.Instance.ItemLinks[ItemToGainIdx].Id, r.Player);
        }
    }

    public override bool ServerUpdate(ChanceEventState r)
    {
        if (ItemToGainIdx == -1)
        {
            int count = r.Player.Items.Count;
            if (count > 0)
            {
                int index = Random.Range(0, count);
                var item = r.Player.Items.ElementAt(index);
                r.Player.Items.Remove(item);
                item.photonView.ViewID = 0;
                r.SendClientStateEvent("RemoveItem", SerializerUtil.SerializeItem(item));
                Object.Destroy(item.gameObject);
            }
        }
        return true;
    }

    public override bool OnClientEvent(ChanceEventState r, IClientEvent e)
    {
        if (ItemToGainIdx == -1)
        {
            if (e is ClientEventStringData ed && ed.Key == "RemoveItem")
            {
                var item = SerializerUtil.DeserializeItem(ed.Data);
                r.Player.Items.Remove(item);
                item.photonView.ViewID = 0;
                Object.Destroy(item.gameObject);
            }
        }
        return true;
    }
}
