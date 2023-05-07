using System.Collections.Generic;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Photon.Pun;

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
                    var t = task;
                    activeOptions = null;
                    task = null;
                    t.SetResult(option.Item1);
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
                    text += option.Item1 + ": " + option.Item2 + '\n';
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

    public Task<GamePlayer> ChoosePlayer(bool allowSelf)
    {
        // client. Do select player
        List<(KeyCode, string)> choices = new List<(KeyCode, string)>();
        int[] map = new int[Game.Instance.PlayerCount - 1];
        const KeyCode START = KeyCode.Alpha1;
        int i = 0;
        foreach (var player in Game.Instance.JoinedPlayers)
        {
            if ((allowSelf || player.PunConnection != PhotonNetwork.LocalPlayer)
                && Game.Instance.PlayerCount != 1) // temp DEBUG
            {
                choices.Add((START+i, player.Name));
                map[i++] = player.Idx;
            }
        }
        return ChooseActionTemp(choices)
            .ContinueWith(t =>
                {
                    Assert.IsTrue(t.IsCompleted);
                    return Game.Instance.IdxToPlayer[map[t.Result - START]];
                },
            TaskContinuationOptions.ExecuteSynchronously);
    }
}