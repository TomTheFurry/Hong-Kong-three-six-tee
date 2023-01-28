using System.Collections;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(UiTempUtil))]
public class PunConnect : MonoBehaviour, IMatchmakingCallbacks
{
    public RectTransform OnJoinRoomMenu = null;

    public void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
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
    }


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
