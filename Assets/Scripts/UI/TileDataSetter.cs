using TMPro;

using UnityEngine;

public class TileDataSetter : MonoBehaviour
{
    public Renderer TextureRenderer;
    public TextMeshPro Name;
    public TextMeshPro Description;

    public void Set(GameTile tile)
    {
        TextureRenderer.material.mainTexture = tile.Image;
        Name.text = tile.Name;
        Description.text = tile.Description;
    }
}
