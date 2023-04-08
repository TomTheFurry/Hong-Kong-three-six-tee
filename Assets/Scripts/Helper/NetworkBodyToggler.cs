using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkBodyToggler : MonoBehaviour
{
    public bool EnableOnHavingBody = false;

    private void Awake()
    {
        Game.OnLocalPlayerTypeChanged.AddListener(OnStateChange);
    }

    private void OnStateChange(Game.LocalPlayerState old, Game.LocalPlayerState state)
    {
        bool useCam = (state & Game.LocalPlayerState.HasNetworkBody) == 0;
        gameObject.SetActive(useCam);
    }

    private void OnDestroy()
    {
        Game.OnLocalPlayerTypeChanged.RemoveListener(OnStateChange);
    }
}
