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
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Button toggleChatButton;
    [SerializeField] private TextMeshProUGUI unreadCountText;
    [SerializeField] private GameObject unreadNotification;

    [Header("Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private bool startMinimized = true;
    [SerializeField] private bool debugMode = true;

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();
    private bool _isChatVisible = false;
    private int _unreadMessages = 0;
    private VivoxManager _vivoxManager;
    private LobbyManager _lobbyManager;
    private bool _isProcessingMessage = false;
    private void Awake()
    {
        _vivoxManager = VivoxManager.Instance;
        _lobbyManager = FindObjectOfType<LobbyManager>();

        if (debugMode)
        {
            Debug.Log($"ChatUI Awake - VivoxManager: {_vivoxManager != null}, LobbyManager: {_lobbyManager != null}");
        }
    }

    private void OnEnable()
    {
        VivoxManager.OnMessageReceivedUI += DisplayNewMessage;

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendButtonClicked);
        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnInputSubmit);
        if (toggleChatButton != null)
            toggleChatButton.onClick.AddListener(ToggleChatVisibility);

        // Suscribir a eventos de Vivox inicializado
        VivoxManager.OnVivoxInitialized += OnVivoxReady;
    }

    private void OnDisable()
    {
        VivoxManager.OnMessageReceivedUI -= DisplayNewMessage;

        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);
        if (toggleChatButton != null)
            toggleChatButton.onClick.RemoveListener(ToggleChatVisibility);

        VivoxManager.OnVivoxInitialized -= OnVivoxReady;
    }

    private void Start()
    {
        StartCoroutine(InitializeChatSystem());
        // Configurar estado inicial del chat
        SetChatVisible(!startMinimized);

        // Verificar estado después de un delay
        StartCoroutine(CheckVivoxStatus());

        // Focus en el input field
        if (chatInputField != null && !startMinimized)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    private IEnumerator CheckVivoxStatus()
    {
        yield return new WaitForSeconds(2f);

        if (debugMode && _vivoxManager != null)
        {
            Debug.Log($"Vivox Status - Initialized: {_vivoxManager.IsInitialized}, " +
                     $"LoggedIn: {_vivoxManager.IsLoggedIn}, " +
                     $"CurrentChannel: {_vivoxManager.CurrentTextChannel}");
        }
    }

    private void OnVivoxReady()
    {
        if (debugMode)
            Debug.Log("ChatUI: Vivox is ready!");
    }

    private void OnSendButtonClicked()
    {
        SendMessageFromInput();
    }

    private void OnInputSubmit(string input)
    {
        SendMessageFromInput();
        // Mantener focus en el input field
        if (chatInputField != null)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    private async void SendMessageFromInput()
    {

        if (_isProcessingMessage) return; // 🔥 PREVENIR DOBLE ENVÍO

        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        _isProcessingMessage = true; // 🔥 BLOQUEAR
        chatInputField.text = "";

        // Verificar si estamos en un lobby
        if (_lobbyManager == null || _lobbyManager.joinLobby == null)
        {
            Debug.LogWarning("Chat: Not in a lobby. Cannot send message.");
            DisplaySystemMessage("You must be in a lobby to use chat.");
            return;
        }

        // Verificar Vivox
        if (_vivoxManager == null)
        {
            Debug.LogError("Chat: VivoxManager not found!");
            DisplaySystemMessage("Chat system is not available.");
            return;
        }

        if (!_vivoxManager.IsInitialized)
        {
            Debug.LogWarning("Chat: Vivox not initialized. Attempting to initialize...");
            await _vivoxManager.InitializeVivox();

            if (!_vivoxManager.IsInitialized)
            {
                DisplaySystemMessage("Failed to initialize chat. Please try again.");
                return;
            }
        }

        // Verificar si estamos en el canal correcto
        string lobbyId = _lobbyManager.joinLobby.Id;
        if (_vivoxManager.CurrentTextChannel != lobbyId)
        {
            Debug.LogWarning($"Chat: Not in correct channel. Current: {_vivoxManager.CurrentTextChannel}, Expected: {lobbyId}");

            // Intentar unirse al canal
            await _vivoxManager.JoinLobbyChannel(lobbyId);

            if (_vivoxManager.CurrentTextChannel != lobbyId)
            {
                DisplaySystemMessage("Unable to connect to lobby chat.");
                return;
            }
        }

        // Procesar comandos
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
            return;
        }

        // Enviar mensaje normal
        try
        {
            await _vivoxManager.SendMessageToChannel(message, lobbyId);

            if (debugMode)
                Debug.Log("Message sent successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error sending message: {ex.Message}");
            DisplaySystemMessage($"Failed to send message: {ex.Message}");
        }
        finally
        {
            _isProcessingMessage = false; // 🔥 DESBLOQUEAR
        }
    }

    private void ProcessCommand(string command)
    {
        if (command.StartsWith("/sendto "))
        {
            // Formato: /sendto "PlayerName" Message
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

                // Buscar ID del jugador por nombre
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
                DisplaySystemMessage("Invalid format. Use: /sendto \"PlayerName\" Message");
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
            DisplaySystemMessage($"Unknown command: {command}. Type /help for commands.");
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
        if (IsDuplicateMessage(message))
        {
            Debug.Log($"Filtering duplicate message: {message.MessageText}");
            return;
        }
        if (debugMode)
            Debug.Log($"New message received: {message.SenderDisplayName}: {message.MessageText}");

        // Si el chat está oculto, incrementar contador de no leídos
        if (!_isChatVisible)
        {
            _unreadMessages++;
            UpdateUnreadNotification();
        }

        // Crear objeto de mensaje
        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        messageGO.transform.SetAsLastSibling();

        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();

        if (messageText != null)
        {
            messageText.text = message.ToString();

            // Aplicar estilos según tipo de mensaje
            if (message.IsDirectMessage)
            {
                messageText.color = Color.magenta;
                messageText.fontStyle = FontStyles.Italic;
            }
            else if (message.SenderDisplayName == "System")
            {
                messageText.color = Color.yellow;
                messageText.fontStyle = FontStyles.Bold;
            }
            else if (message.SenderDisplayName == AuthenticationService.Instance.PlayerName)
            {
                messageText.color = Color.cyan;
            }
            else
            {
                messageText.color = Color.white;
            }
        }

        _spawnedChatMessages.Add(messageGO);

        // Limitar número de mensajes
        if (_spawnedChatMessages.Count > maxMessages)
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        // Auto-scroll al final
        StartCoroutine(ForceScrollDown());
    }
    private bool IsDuplicateMessage(ChatMessage newMessage)
    {
        // Verificar si el último mensaje es igual (simple prevención)
        if (_spawnedChatMessages.Count > 0)
        {
            var lastMessageGO = _spawnedChatMessages[^1];
            if (lastMessageGO != null)
            {
                var lastText = lastMessageGO.GetComponent<TextMeshProUGUI>()?.text;
                var newText = newMessage.ToString();

                if (lastText == newText)
                {
                    return true;
                }
            }
        }
        return false;
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
        yield return new WaitForEndOfFrame(); // Frame extra para asegurar

        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();

            // Forzar rebuild del layout
            if (chatScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            }

            // Scroll al final
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ToggleChatVisibility()
    {
        SetChatVisible(!_isChatVisible);
    }

    public void SetChatVisible(bool visible)
    {
        _isChatVisible = visible;
        chatPanel.SetActive(visible);

        if (visible)
        {
            _unreadMessages = 0;
            UpdateUnreadNotification();

            // Focus en el input field
            if (chatInputField != null)
            {
                chatInputField.Select();
                chatInputField.ActivateInputField();
            }
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
    private IEnumerator InitializeChatSystem()
    {
        yield return new WaitForSeconds(1f);

        if (_vivoxManager == null)
        {
            _vivoxManager = FindObjectOfType<VivoxManager>();
            if (_vivoxManager == null)
            {
                GameObject vivoxObj = new GameObject("VivoxManager");
                _vivoxManager = vivoxObj.AddComponent<VivoxManager>();
                DontDestroyOnLoad(vivoxObj);
            }
        }

        if (!_vivoxManager.IsInitialized)
        {
            // CORRECCIÓN: No usar await en coroutine normal
            // En su lugar, inicia la tarea y espera que termine
            var initTask = _vivoxManager.InitializeVivox();

            while (!initTask.IsCompleted)
            {
                yield return null; // Esperar un frame
            }

            yield return new WaitForSeconds(2f);
        }

        Debug.Log("Chat system initialized: " + _vivoxManager.IsInitialized);
    }

    private void ClearChatMessages()
    {
        foreach (GameObject msg in _spawnedChatMessages)
        {
            if (msg != null) Destroy(msg);
        }
        _spawnedChatMessages.Clear();
    }
}