using System.Threading.Tasks;

using TMPro;

using Unity.VisualScripting;

using UnityEngine;

public class TileDataSetter : MonoBehaviour
{
    public Renderer TextureRenderer;
    public TextMeshPro Name;
    public TextMeshPro Description;
    private Task<Texture2D> _textureTask;

    public void Set(GameTile tile)
    {
        Debug.Log($"Setting tile {tile.Name}");
        _textureTask = tile.ImageLoaded.Task;
        Name.text = tile.Name;
        Description.text = tile.Description;
    }

    private void Update()
    {
        if (_textureTask != null && _textureTask.IsCompleted)
        {
            Material mat = new Material(TextureRenderer.material);
            mat.mainTexture = _textureTask.Result;
            TextureRenderer.material = mat;
            _textureTask = null;
        }
    }
}
