using System.Collections.Generic;

using Photon.Pun;

using TMPro;

using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class PieceSelector : MonoBehaviour
{
    int selectedPiece = -1;
    private List<int> pieces = new();

    void Start()
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    void Update()
    {
        pieces.Clear();
        for (int i = 0; i < Game.Instance.PiecesTemplate.Length; i++)
        {
            var piece = Game.Instance.PiecesTemplate[i];
            if (piece.Owner == null || piece.Owner.PunConnection == PhotonNetwork.LocalPlayer)
            {
                pieces.Add(i);
            }
        }

        var dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(pieces.ConvertAll(i => "Piece " + i));

        if (selectedPiece != -1)
        {
            int newSlot = pieces.FindIndex(i => i == selectedPiece);
            if (newSlot != -1)
            {
                dropdown.SetValueWithoutNotify(newSlot);
            }
            else
            {
                dropdown.SetValueWithoutNotify(-1);
                selectedPiece = -1;
            }
        }
    }

    void OnValueChanged(int value)
    {
        selectedPiece = value == -1 ? -1 : pieces[value];
        Game.Instance.photonView.RPC(nameof(Game.ClientTrySelectPiece), RpcTarget.MasterClient, selectedPiece);
    }
}
