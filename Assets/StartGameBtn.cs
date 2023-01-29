using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameBtn : MonoBehaviour
{
    Button btn;
    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(() =>
            {
                if (Game.Instance.State is StateStartup startup) startup.MasterSignalStartGame = true;
            }
        );
    }

    void Update()
    {
        btn.interactable = Game.Instance.State is StateStartup startup && startup.CanStart;

        if (Game.Instance.State is StateChooseOrder chooseOrder)
        {
            GetComponent<UiTempUtil>().SetActiveMenu(null); // todo
        }
    }
}
