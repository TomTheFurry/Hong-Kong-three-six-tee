using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using TMPro;

using UnityEngine;
using UnityEngine.Assertions;

public class DebugKeybind : MonoBehaviour
{
    public static DebugKeybind Instance;

    [CanBeNull]
    public TextMeshProUGUI debugText;

    [CanBeNull]
    public IEnumerable<(KeyCode, string)> activeOptions = null;

    private TaskCompletionSource<KeyCode> task = null;

    private void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }

    public void Update()
    {
        if (activeOptions != null)
        {
            foreach (var option in activeOptions)
            {
                if (Input.GetKeyDown(option.Item1))
                {
                    task.SetResult(option.Item1);
                    activeOptions = null;
                    task = null;
                    break;
                }
            }
        }

        if (debugText != null)
        {
            if (activeOptions != null)
            {
                string text = "";

                text += "Active Options: \n";
                foreach (var option in activeOptions)
                {
                    text += option.Item1 + ": " + option.Item2;
                }
                debugText.text = text;
            }
            else
            {
                debugText.text = "";
            }
        }
    }

    [NotNull]
    public Task<KeyCode> ChooseActionTemp(IEnumerable<(KeyCode, string)> options)
    {
        Assert.IsNull(activeOptions);
        Assert.IsNull(task);
        Debug.Log("ChooseActionTemp");

        activeOptions = options;
        task = new TaskCompletionSource<KeyCode>();
        return task.Task;
    }
}