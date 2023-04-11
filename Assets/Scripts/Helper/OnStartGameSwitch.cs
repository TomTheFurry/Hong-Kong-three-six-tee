using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnStartGameSwitch : MonoBehaviour
{
    void Update()
    {
        if (Game.Instance.State is StateRollOrder chooseOrder)
        {
            GetComponent<UiTempUtil>().SetActiveMenu(null); // hide it.
        }
    }
}
