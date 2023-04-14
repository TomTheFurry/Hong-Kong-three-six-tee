using System.Collections;

using UnityEngine;

[CreateAssetMenu(fileName = "TileType", menuName = "TileType", order = 1)]
public class TileType : ScriptableObject
{
    public string Name;
    public Material Color;
    public bool Ownable;
}