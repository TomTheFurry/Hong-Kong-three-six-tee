using UnityEngine;

[RequireComponent(typeof(PcControl))]
public class PcManager : MonoBehaviour
{
    public Canvas CanvasUI;

    private Vector3 LocalOffset;
    private Quaternion LocalRot;

    private Quaternion CanvasRotation;
    private Vector3 CanvasAbsOffset;
    PcControl pc;
    private float time = 0;

    void Start()
    {
        time = Time.time;
        LocalOffset = CanvasUI.transform.localPosition;
        LocalRot = CanvasUI.transform.localRotation;

        CanvasRotation = CanvasUI.transform.rotation;
        CanvasAbsOffset = CanvasUI.transform.position - transform.position;
        //Debug.Log($"CanvasOffect: {CanvasOffect}");
        pc = GetComponent<PcControl>();
    }

    public void Update()
    {
        if (Time.time - time > 1)
        {
            time = Time.time;
            ShowUi();
        }
        CanvasUI.transform.position = transform.position + CanvasAbsOffset;
        CanvasUI.transform.rotation = CanvasRotation;
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

    public void ShowUi()
    {
        CanvasUI.transform.localPosition = LocalOffset;
        CanvasUI.transform.localRotation = LocalRot;

        CanvasRotation = CanvasUI.transform.rotation;
        CanvasAbsOffset = CanvasUI.transform.position - transform.position;
    }
}
