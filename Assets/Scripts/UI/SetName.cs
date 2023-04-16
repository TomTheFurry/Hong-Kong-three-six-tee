using Photon.Pun;

using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_InputField))]
public class SetName : MonoBehaviour
{
    void Start()
    {
        var input = GetComponent<TMPro.TMP_InputField>();
        input.text = PhotonNetwork.NickName;
        input.onEndEdit.AddListener((string name) => PhotonNetwork.NickName = name);
    }

}
