using Photon.Pun;

using UnityEngine;

public class UiTempUtil : MonoBehaviour
{
    public static RectTransform ActiveMenu = null;

    public void SetActiveMenu(RectTransform menu)
    {
        if (ActiveMenu != null)
        {
            ActiveMenu.gameObject.SetActive(false);
        }
        ActiveMenu = menu;
        ActiveMenu?.gameObject.SetActive(true);
    }

    public bool EnableOnlyIfMaster = false;
    public bool SetAsActiveMenuOnStart = false;

    void Start()
    {
        if (EnableOnlyIfMaster && !PhotonNetwork.IsMasterClient)
        {
            gameObject.SetActive(false);
        }
        else if (SetAsActiveMenuOnStart)
        {
            SetActiveMenu(GetComponent<RectTransform>());
        }
    }
}
