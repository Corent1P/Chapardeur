using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Authentication;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private Button showChatButton;
    [SerializeField] private Button hideChatButton;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;
    //[SerializeField] private Button toggleChatButton;
    [SerializeField] private TextMeshProUGUI unreadCountText;
    [SerializeField] private GameObject unreadNotification;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private bool startMinimized = false;

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();
    private bool _isChatVisible = true;
    private int _unreadMessages = 0;
    private VivoxManager _vivoxManager;
    private LobbyManager _lobbyManager;

    private void Awake()
    {
        _vivoxManager = FindObjectOfType<VivoxManager>();
        _lobbyManager = FindObjectOfType<LobbyManager>();

        if (startMinimized)
        {
            ToggleChatVisibility();
        }
    }
    private void Start()
    {
        // Inicialmente ocultar el chat
        chatPanel.SetActive(false);
        showChatButton.gameObject.SetActive(true);
        hideChatButton.gameObject.SetActive(false);

        // Configurar botones
        showChatButton.onClick.AddListener(ShowChat);
        hideChatButton.onClick.AddListener(HideChat);
    }

    private void OnEnable()
    {
        VivoxManager.OnMessageReceivedUI += DisplayNewMessage;

        if (sendButton != null) sendButton.onClick.AddListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.AddListener(OnInputSubmit);
      //  if (toggleChatButton != null) toggleChatButton.onClick.AddListener(ToggleChatVisibility);

        // Subscribe to lobby events
        if (_lobbyManager != null)
        {
            // We'll need to check lobby state changes
            StartCoroutine(CheckLobbyState());
        }
    }

    private void OnDisable()
    {
        VivoxManager.OnMessageReceivedUI -= DisplayNewMessage;

        if (sendButton != null) sendButton.onClick.RemoveListener(OnSendButtonClicked);
        if (chatInputField != null) chatInputField.onSubmit.RemoveListener(OnInputSubmit);
        //if (toggleChatButton != null) toggleChatButton.onClick.RemoveListener(ToggleChatVisibility);
    }

    private IEnumerator CheckLobbyState()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (_lobbyManager.joinLobby == null)
            {
                ClearChatMessages();
            }
        }
    }

    private void ShowChat()
    {
        chatPanel.SetActive(true);
        showChatButton.gameObject.SetActive(false);
        hideChatButton.gameObject.SetActive(true);
    }

    private void HideChat()
    {
        chatPanel.SetActive(false);
        showChatButton.gameObject.SetActive(true);
        hideChatButton.gameObject.SetActive(false);
    }
    private void OnSendButtonClicked()
    {
        SendMessageFromInput();
    }

    private void OnInputSubmit(string input)
    {
        SendMessageFromInput();
        chatInputField.ActivateInputField();
    }

    private async void SendMessageFromInput()
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        chatInputField.text = "";

        // Check if we're in a lobby
        if (_lobbyManager == null || _lobbyManager.joinLobby == null)
        {
            Debug.LogWarning("Chat: Not in a lobby. Cannot send message.");
            return;
        }

        // Check Vivox
        if (_vivoxManager == null || !_vivoxManager.IsInitialized)
        {
            Debug.LogWarning("Chat: Vivox not initialized. Cannot send message.");
            return;
        }

        // Check for commands
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
            return;
        }

        // Send normal message
        string channelName = _lobbyManager.joinLobby.Id;
        await _vivoxManager.SendMessageToChannel(message, channelName);
    }

    private void ProcessCommand(string command)
    {
        if (command.StartsWith("/sendto "))
        {
            // Parse: /sendto "PlayerName" Message
            string[] parts = command.Substring("/sendto ".Length).Split(new[] { '"' }, 3);

            if (parts.Length >= 3)
            {
                string targetName = parts[1];
                string messageContent = parts[2].Trim();

                if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(messageContent))
                {
                    DisplaySystemMessage("Usage: /sendto \"PlayerName\" Message");
                    return;
                }

                // Find player ID by name
                string playerId = GetPlayerIdByName(targetName);
                if (!string.IsNullOrEmpty(playerId))
                {
                    _vivoxManager.SendDirectMessage(messageContent, playerId);
                }
                else
                {
                    DisplaySystemMessage($"Player '{targetName}' not found.");
                }
            }
            else
            {
                DisplaySystemMessage("Invalid command format. Use: /sendto \"PlayerName\" Message");
            }
        }
        else if (command == "/help")
        {
            DisplaySystemMessage("Chat commands:");
            DisplaySystemMessage("/sendto \"PlayerName\" Message - Send private message");
            DisplaySystemMessage("/help - Show this help");
        }
        else
        {
            DisplaySystemMessage($"Unknown command: {command}. Type /help for available commands.");
        }
    }

    private string GetPlayerIdByName(string playerName)
    {
        if (_lobbyManager == null || _lobbyManager.joinLobby == null) return null;

        foreach (var player in _lobbyManager.joinLobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                if (player.Data["PlayerName"].Value == playerName)
                {
                    return player.Id;
                }
            }
        }
        return null;
    }

    private void DisplayNewMessage(ChatMessage message)
    {
        // If chat is hidden, increment unread count
        if (!_isChatVisible)
        {
            _unreadMessages++;
            UpdateUnreadNotification();
        }

        // Create message UI
        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();

        if (messageText != null)
        {
            messageText.text = message.ToString();

            // Color code messages
            if (message.IsDirectMessage)
            {
                messageText.color = Color.magenta;
            }
            else if (message.SenderDisplayName == PlayerDataManager.Instance.PlayerName)
            {
                messageText.color = Color.cyan;
            }
        }

        _spawnedChatMessages.Add(messageGO);

        // Limit number of messages
        if (_spawnedChatMessages.Count > maxMessages)
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        // Auto-scroll
        StartCoroutine(ForceScrollDown());
    }

    private void DisplaySystemMessage(string message)
    {
        var systemMessage = new ChatMessage
        {
            SenderDisplayName = "System",
            MessageText = message,
            IsDirectMessage = false
        };
        DisplayNewMessage(systemMessage);
    }

    private IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ClearChatMessages()
    {
        foreach (GameObject msg in _spawnedChatMessages)
        {
            Destroy(msg);
        }
        _spawnedChatMessages.Clear();
    }

    private void ToggleChatVisibility()
    {
        _isChatVisible = !_isChatVisible;
        chatPanel.SetActive(_isChatVisible);

        if (_isChatVisible)
        {
            _unreadMessages = 0;
            UpdateUnreadNotification();
        }
    }

    private void UpdateUnreadNotification()
    {
        if (unreadNotification != null)
        {
            unreadNotification.SetActive(_unreadMessages > 0);
        }

        if (unreadCountText != null)
        {
            unreadCountText.text = _unreadMessages > 99 ? "99+" : _unreadMessages.ToString();
        }
    }

    // Public method to show/hide chat
    public void SetChatVisible(bool visible)
    {
        _isChatVisible = visible;
        chatPanel.SetActive(visible);

        if (visible)
        {
            _unreadMessages = 0;
            UpdateUnreadNotification();
        }
    }
}