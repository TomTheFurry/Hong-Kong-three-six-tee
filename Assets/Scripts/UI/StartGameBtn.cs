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
    }
}
