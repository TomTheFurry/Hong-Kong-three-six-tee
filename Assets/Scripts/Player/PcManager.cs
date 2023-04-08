using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PcControl))]
public class PcManager : MonoBehaviour
{
    public Canvas CanvasUI;
    PcControl pc;
    void Start()
    {
        pc = GetComponent<PcControl>();
    }

    public void Update()
    {
        GameState state = Game.Instance.State;
        bool allowInput = state is not StateStartup;
        pc.ProcessKeyboardAction = allowInput;
        pc.ProcessMouseAction = allowInput;
        if (pc.ProcessMouseAction)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }
}
