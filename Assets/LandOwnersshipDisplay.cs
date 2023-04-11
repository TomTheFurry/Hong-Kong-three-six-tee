using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;

[RequireComponent(typeof(LandOwnershipItem))]
public class LandOwnersshipDisplay : MonoBehaviour
{
    public TextMeshPro Title;
    public TextMeshPro Description;

    private LandOwnershipItem Item;
    
    void Start()
    {
        Item = GetComponent<LandOwnershipItem>();
    }
    
    void Update()
    {
        Title.text = Item.Tile.Name;
        Description.text = "TODO!";
    }
}
