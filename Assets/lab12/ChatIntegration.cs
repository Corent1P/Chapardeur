using UnityEngine;

public class ChatIntegration : MonoBehaviour
{
    [SerializeField] private ChatUI chatUI;
    [SerializeField] private VivoxManager vivoxManager;

    private void Start()
    {
        if (vivoxManager && chatUI)
        {
            vivoxManager.OnChannelMessageReceived += HandleChannelMessage;
            vivoxManager.OnDirectMessageReceived += HandleDirectMessage;
        }
    }

    private void HandleChannelMessage(string sender, string message, string channel)
    {
        chatUI.AddMessageToChat(sender, message, false);
    }

    private void HandleDirectMessage(string sender, string message, string channel)
    {
        chatUI.AddMessageToChat(sender, message, true);
    }

    private void OnDestroy()
    {
        if (vivoxManager)
        {
            vivoxManager.OnChannelMessageReceived -= HandleChannelMessage;
            vivoxManager.OnDirectMessageReceived -= HandleDirectMessage;
        }
    }
}