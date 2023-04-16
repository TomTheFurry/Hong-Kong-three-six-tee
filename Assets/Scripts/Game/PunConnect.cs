using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

[RequireComponent(typeof(UiTempUtil))]
public class PunConnect : MonoBehaviour, IMatchmakingCallbacks
{
    public RectTransform OnJoinRoomMenu = null;
    public GameObject BeforeConnected;
    public GameObject AfterConnected;

    private void SetConnected()
    {
        bool connected = PhotonNetwork.InLobby;
        BeforeConnected.SetActive(!connected);
        AfterConnected.SetActive(connected);

    }

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        SetConnected();
    }

    public void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void AssertState()
    {
        Debug.Assert(PhotonNetwork.InLobby);
    }
    
    public void QuickConnect()
    {
        AssertState();
        PhotonNetwork.JoinRandomOrCreateRoom();
        SetConnected();
    }

    void Update() { SetConnected(); }

    public void OnJoinedRoom()
    {
        if (OnJoinRoomMenu != null)
        {
            GetComponent<UiTempUtil>().SetActiveMenu(OnJoinRoomMenu);
        }
    }
    
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    public void OnCreatedRoom() { }
    public void OnCreateRoomFailed(short returnCode, string message) { }
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    public void OnLeftRoom() { }
}
