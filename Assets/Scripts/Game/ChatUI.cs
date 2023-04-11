using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.InputSystem;

public class ChatUI : MonoBehaviour
{
    public TextMeshProUGUI chatText;
    public TMP_InputField chatInput;

    public InputActionReference StartChatAction;

    private Queue<(float realtime, string message)> messages = new();

    private void Start()
    {
        StartChatAction.action.performed += StartChat;
        Chat.Instance.OnRecieveMessage.AddListener(Receive);
        chatInput.onEndEdit.AddListener(OnChatInputEndEdit);
        chatInput.onDeselect.AddListener(CloseChat);
    }

    private void OnDestroy()
    {
        StartChatAction.action.performed -= StartChat;
    }

    private void StartChat(InputAction.CallbackContext obj)
    {
        Debug.Log("StartChat Toggled");
        if (chatInput.IsActive())
        {
            chatInput.gameObject.SetActive(false);
            chatInput.DeactivateInputField();
        }
        else
        {
            chatInput.gameObject.SetActive(true);
            chatInput.Select();
            chatInput.ActivateInputField();
        }
    }

    private void CloseChat(string text)
    {
        chatInput.gameObject.SetActive(false);
    }

    public void OnChatInputEndEdit(string text)
    {
        if (text.Length > 0)
        {
            Debug.Log($"Send text {text}");
            Chat.Instance.send(text);
        }
        chatInput.text = "";
        chatInput.gameObject.SetActive(false);
    }

    public void Receive(string message)
    {
        messages.Enqueue((Time.realtimeSinceStartup, message));
    }

    private void Update()
    {
        while (messages.Count > 0 && messages.Peek().realtime < Time.realtimeSinceStartup - 10)
        {
            messages.Dequeue();
        }
        chatText.text = string.Join("\n", messages.Select(p => p.message));
    }
}
