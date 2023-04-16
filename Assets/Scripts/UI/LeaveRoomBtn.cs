using Photon.Pun;

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LeaveRoomBtn : MonoBehaviour
{
    public RectTransform Menu;
    Button btn;
    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(() =>
            {
                PhotonNetwork.LeaveRoom();
                GetComponent<UiTempUtil>().SetActiveMenu(Menu);
            }
        );
    }
}
