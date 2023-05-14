using JetBrains.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(PcControl))]
public class PcManager : MonoBehaviour
{
    public Transform UI;

    private Vector3 LocalOffset;
    private Quaternion LocalRot;

    private Quaternion CanvasRotation;
    private Vector3 CanvasAbsOffset;
    PcControl pc;

    private PlayerUiShop ShopUi;

    void Start()
    {
        LocalOffset = UI.localPosition;
        LocalRot = UI.localRotation;

        CanvasRotation = UI.rotation;
        CanvasAbsOffset = UI.position - transform.position;
        //Debug.Log($"CanvasOffect: {CanvasOffect}");
        pc = GetComponent<PcControl>();

        ShopUi = UI.GetComponentInChildren<PlayerUiShop>();
        GetShopUi().init();
    }

    public void Update()
    {
        if (!PlayerUiIcon.IsHovered) PlayerUiIcon.PlayerUiIconHovered = null;
        PlayerUiIcon.IsHovered = false;

        UI.position = transform.position + CanvasAbsOffset;
        UI.rotation = CanvasRotation;
        GameState state = Game.Instance.State;
        bool allowInput = state is not StateStartup;
        pc.ProcessKeyboardAction = allowInput;
        pc.ProcessMouseAction = allowInput;
        if (pc.ProcessMouseAction)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    public PlayerUiShop GetShopUi()
    {
        return ShopUi;
    }

    public void HideUi()
    {
        foreach (Transform obj in UI.transform)
        {
            obj.gameObject.SetActive(false);
        }
        UI.gameObject.SetActive(true);
    }

    public void ShowUi(GameObject gameObject = null)
    {
        foreach (Transform obj in UI.transform)
        {
            obj.gameObject.SetActive(false);
        }
        if (gameObject != null) gameObject.SetActive(true);
        UI.gameObject.SetActive(true);

        UI.localPosition = LocalOffset;
        UI.localRotation = LocalRot;

        CanvasRotation = UI.rotation;
        CanvasAbsOffset = UI.position - transform.position;
    }
}