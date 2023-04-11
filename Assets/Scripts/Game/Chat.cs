using System.Collections;

using JetBrains.Annotations;

using Photon.Pun;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class Chat : MonoBehaviourPun
{
    public static Chat Instance;
    public UnityEvent<string> OnRecieveMessage = new();

    void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    public void send(string message)
    {
        Debug.Log($"Sending msg {message}");
        photonView.RPC("MessageSent", RpcTarget.All, message);
    }


    [PunRPC]
    public void MessageSent(string message, PhotonMessageInfo info)
    {
        Debug.Log($"Recieved msg {message}");
        OnRecieveMessage.Invoke($"{info.Sender.NickName}: {message}");
    }

}