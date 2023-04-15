using System.Collections;
using System.Collections.Generic;

using Photon.Pun;
using Photon.Realtime;

using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public abstract class PlayerObjBase : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public GamePlayer gamePlayer { get; private set; }
    
    public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // the Player should already be created, and thus the tag object should already be instantiated
        gamePlayer = info.photonView.Owner;
        gamePlayer.PlayerObj = this;
        Debug.Log($"{gamePlayer} instantiated a {this}");
        if (gamePlayer.PunConnection == PhotonNetwork.LocalPlayer)
        {
            Game.Instance.LocalState |= Game.LocalPlayerState.HasNetworkBody;
        }
    }

    public void OnDestroy()
    {
        if (gameObject == null) return;
        gamePlayer.PlayerObj = null;
        if (gamePlayer.PunConnection == PhotonNetwork.LocalPlayer)
        {
            Game.Instance.LocalState &= ~Game.LocalPlayerState.HasNetworkBody;
        }
    }

    public virtual void SetMaterial(Material material) {
        foreach (MeshRenderer mesh in GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material = material;
        }
    }
}