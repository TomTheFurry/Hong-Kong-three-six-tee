using System.Collections.Generic;
using System.Threading.Tasks;

using ExitGames.Client.Photon;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

public partial class Game : MonoBehaviourPun, IInRoomCallbacks, IConnectionCallbacks, IPunPrefabPool, IMatchmakingCallbacks
{
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        GamePlayer p = new GamePlayer(newPlayer);
        Debug.Log($"Player {p} joined the game");
        JoinedPlayersLock.EnterWriteLock();
        bool v = JoinedPlayers.Add(p);
        JoinedPlayersLock.ExitWriteLock();
        Debug.Assert(v, "Dup player detected!");
        OnPlayerJoinedCheckHash(newPlayer);
        if (photonView.IsMine)
        {
            PushRPCEvent(new RPCEventNewPlayer{GamePlayer = newPlayer});
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer.TagObject is not GamePlayer player) return;
        JoinedPlayersLock.EnterWriteLock();
        if (IsMaster && player.Piece != null)
        {
            player.Piece.SetOwner(null);
        }
        if (IsMaster && player.PlayerObj != null)
        {
            Debug.Log("Backup method removing left player obj");
            //PhotonNetwork.Destroy(player.PlayerObj.gameObject); // TODO: Fix these removal for rejoin logic
        }
        try
        {
            bool v = JoinedPlayers.Remove(player);
            Debug.Assert(v, "Player not found!");
        }
        finally
        {
            JoinedPlayersLock.ExitWriteLock();
        }
    }
    
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }

    public void OnCreatedRoom()
    {
    }
    
    public void OnCreateRoomFailed(short returnCode, string message) { }
    
    public void OnJoinedRoom() {
        JoinedPlayersLock.EnterWriteLock();
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            GamePlayer p = new GamePlayer(player);
            bool v = JoinedPlayers.Add(p);
        }
        JoinedPlayersLock.ExitWriteLock();

        var state = LocalPlayerState.InRoom;
        if (IsMaster)
        {
            state |= LocalPlayerState.IsMaster;
        }
        LocalState |= state;

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Transform spawnpoint = SpawnpointsArr[(playerCount-1) % SpawnpointsArr.Length];
        //TODO: If vr, spawn vr control
        PhotonNetwork.Instantiate("PlayerPc", spawnpoint.position, spawnpoint.rotation);
    }
    
    public void OnJoinRoomFailed(short returnCode, string message) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }

    public void OnLeftRoom()
    {
        if (PhotonNetwork.LocalPlayer.TagObject is GamePlayer player)
        {
            if (player.Piece != null) player.Piece.Owner = null;
            Object.Destroy(player.PlayerObj);
            player.PlayerObj = null;
        }
        PhotonNetwork.LocalPlayer.TagObject = null;
        LocalState = LocalPlayerState.NotPlaying;
    }
    
    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            LocalState |= LocalPlayerState.IsMaster;
        }
        else
        {
            LocalState &= ~LocalPlayerState.IsMaster;
        }
        PhotonNetwork.Disconnect(); // for now cant handle master switch
    }


    public void OnConnected()
    {
        Debug.Log("Connection made to Server");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("Connected to master");
        PhotonNetwork.JoinLobby();
    }
    
    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected: {cause}");
        if (cause != DisconnectCause.ApplicationQuit &&
            cause != DisconnectCause.DisconnectByClientLogic &&
            cause != DisconnectCause.DisconnectByServerLogic &&
            cause != DisconnectCause.DisconnectByDisconnectMessage &&
            cause != DisconnectCause.DisconnectByOperationLimit)
        {
            Debug.Log($"Retrying connection in 3 seconds...");
            Task.Run(
                async () =>
                {
                    await Task.Delay(3000);
                    Debug.Log($"Connecting...");
                    PhotonNetwork.ConnectUsingSettings();
                }
            );
        }
    }
    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (PunPrefabs.TryGetValue(prefabId, out var prefab))
        {
            if (prefab.activeInHierarchy)
            {
                Debug.LogError($"PunPrefab requires the prefab to be set and saved as inactive. Prefab: [{prefab.name}])");
                prefab.SetActive(false);
            }
            return Instantiate(prefab, position, rotation);
        }
        else
        {
            Debug.LogError($"Prefab [{prefabId}] not found in registered prefabs. Did you forget to register it?");
            return null;
        }
    }

    public void Destroy(GameObject gameObject)
    {
        Object.Destroy(gameObject);
    }
}
