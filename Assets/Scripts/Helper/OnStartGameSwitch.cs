using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnStartGameSwitch : MonoBehaviour
{
    void Update()
    {
        if (Game.Instance.State is StateChooseOrder chooseOrder)
        {
            GetComponent<UiTempUtil>().SetActiveMenu(null); // todo
        }
    }
}
