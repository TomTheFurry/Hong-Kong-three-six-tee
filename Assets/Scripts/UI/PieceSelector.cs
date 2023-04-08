using System.Collections;
using System.Collections.Generic;

using Photon.Pun;

using TMPro;

using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class PieceSelector : MonoBehaviour
{
    void Start()
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(int value)
    {
        Game.Instance.photonView.RPC("ClientTrySelectPiece", RpcTarget.MasterClient, value);
    }
}
