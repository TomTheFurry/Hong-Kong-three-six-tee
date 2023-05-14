using cakeslice;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerUiShop : MonoBehaviour
{
    private List<PlayerUiIcon> icons;

    [CanBeNull]
    public IEnumerable<(GameItem, double, int)> activeOptions = null;

    private TaskCompletionSource<int> task = null;

    private PcManager pcManager;
    public bool IsRequireSelect => task != null;

    public void init()
    {
        icons = new List<PlayerUiIcon>();
        pcManager = GetComponentInParent<PcManager>();
        foreach (Transform icon in transform)
        {
            PlayerUiIcon playerUiIcon = icon.gameObject.AddComponent<PlayerUiIcon>();
            foreach (MeshRenderer meshRenderer in icon.GetComponentsInChildren<MeshRenderer>())
            {
                if (meshRenderer.name == "Nametag")
                {
                    playerUiIcon.Text = meshRenderer.GetComponent<TMPro.TMP_Text>();
                    continue;
                }
                if (meshRenderer.GetComponent<BoxCollider>() == null) meshRenderer.AddComponent<BoxCollider>();
                Outline ol = meshRenderer.gameObject.AddComponent(typeof(Outline)) as Outline;
                ol.color = 2;
                ol.eraseRenderer = true; // true is not show, false is show
            }
            icons.Add(playerUiIcon);
        }
        gameObject.SetActive(false);
    }

    public Task<int> ShowShop(IEnumerable<(GameItem, double, int)> options)
    {
        Assert.IsNull(activeOptions);
        Assert.IsNull(task);
        Debug.Log("ShowShopUi");

        activeOptions = options;
        task = new TaskCompletionSource<int>();

        // show icon
        UpdateShop();

        if (!gameObject.activeSelf) pcManager.ShowUi(gameObject);

        return task.Task;
    }

    public void UpdateShop()
    {
        foreach (var icon in icons)
        {
            icon.gameObject.SetActive(false);
        }

        List<(PlayerUiIcon, string, int)> iconsList = new List<(PlayerUiIcon, string, int)>();
        iconsList.Add((icons.FirstOrDefault(icon => icon.name == "Exit"), "", -1));
        foreach (var option in activeOptions)
        {
            iconsList.Add((icons.FirstOrDefault(icon => icon.name == option.Item1.Name), option.Item2.ToString(), option.Item3));
        }

        int iconCount = iconsList.Count;
        float xOffset = iconCount % 2 == 0 ? (iconCount / 2f - 0.5f) : 0f;
        for (int i = 0; i < iconCount; i++)
        {
            int index = iconsList[i].Item3;
            PlayerUiIcon icon = iconsList[i].Item1;
            icon.SetText(iconsList[i].Item2);
            icon.SetCallBack(() => {
                var t = task;
                activeOptions = null;
                task = null;
                t.SetResult(index);
            });
            Vector3 position = icon.transform.localPosition;
            position.x = i - xOffset;
            icon.transform.localPosition = position;
            icon.gameObject.SetActive(true);
        }
    }
}
