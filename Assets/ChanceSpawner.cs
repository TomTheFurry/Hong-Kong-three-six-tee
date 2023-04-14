using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ChanceSpawner : MonoBehaviour
{
    public static ChanceSpawner Instance;

    public List<ChanceCard> Cards;

    public void Start()
    {
        Instance = this;
        Cards = ItemTemplateDefiner.Instance.ItemTemplate
            .Where(x => x is ChanceCard)
            .Cast<ChanceCard>().ToList();
    }

    public ChanceCard DrawCard()
    {
        var card = Cards[Random.Range(0, Cards.Count)];
        int typeId = card.Id;
        var dup = ItemTemplateDefiner.Instance.ServerInstantiateItem(typeId);
        return dup as ChanceCard;
    }
}
