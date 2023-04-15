using System.Linq;

using JetBrains.Annotations;

using UnityEngine;

[CreateAssetMenu(fileName = "ChanceEventItem", menuName = "ChanceEvent/Item")]
public class ChanceEventItem : ChanceEvent
{
    [CanBeNull]
    [SerializeReference]
    public ItemBase ItemToGain;

    public override void OnCreate(ChanceEventState r)
    {
        if (!r.IsMaster) return;

        if (ItemToGain != null)
        {
            ItemTemplateDefiner.Instance.ServerInstantiateItem(ItemToGain.Id, r.Player);
        }
    }

    public override bool ServerUpdate(ChanceEventState r)
    {
        if (ItemToGain == null)
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
        if (ItemToGain == null)
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
