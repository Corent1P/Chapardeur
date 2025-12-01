using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class ChatUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject systemMessagePrefab;
    [SerializeField] private GameObject privateMessagePrefab;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private int maxVisibleMessages = 100;

    [Header("Colors")]
    [SerializeField] private Color normalMessageColor = Color.white;
    [SerializeField] private Color systemMessageColor = Color.cyan;
    [SerializeField] private Color privateMessageColor = Color.yellow;
    [SerializeField] private Color playerNameColor = new Color(0.2f, 0.8f, 1f);

    private List<GameObject> messageInstances = new List<GameObject>();
    private bool isChatActive = false;
    private Dictionary<string, Color> playerColors = new Dictionary<string, Color>();
    private bool isTyping = false;

    void Start()
    {
        // Configurar UI inicial
        isChatActive = !startHidden;
        chatWindow.SetActive(isChatActive);

        // Event listeners
        sendButton.onClick.AddListener(OnSendClicked);
        messageInput.onSubmit.AddListener(OnMessageSubmit);
        messageInput.onSelect.AddListener(OnInputSelected);
        messageInput.onDeselect.AddListener(OnInputDeselected);

        if (closeButton != null)
            closeButton.onClick.AddListener(ToggleChat);

        // Suscribirse al ChatManager
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived += DisplayMessage;
            ChatManager.Instance.OnChatCleared += ClearMessages;
            ChatManager.Instance.OnChatInitialized += OnChatInitialized;
        }

        // Suscribirse al LobbyManager
        var lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            // Usar eventos personalizados si los tienes
        }
    }

    void OnDestroy()
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived -= DisplayMessage;
            ChatManager.Instance.OnChatCleared -= ClearMessages;
            ChatManager.Instance.OnChatInitialized -= OnChatInitialized;
        }
    }

    void Update()
    {
        // Usar Input System en lugar de UnityEngine.Input
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        // Toggle del chat con tecla T
        if (keyboard.tKey.wasPressedThisFrame)
        {
            ToggleChat();
        }

        // Enviar con Enter (solo si no está escribiendo multilínea con Shift)
        if (isChatActive && keyboard.enterKey.wasPressedThisFrame)
        {
            bool shiftPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

            if (!shiftPressed && !string.IsNullOrWhiteSpace(messageInput.text))
            {
                OnSendClicked();
            }
            else if (!shiftPressed && !isTyping)
            {
                // Si el input está vacío y no estamos escribiendo, activarlo
                messageInput.Select();
                messageInput.ActivateInputField();
            }
        }

        // Cerrar chat con Escape
        if (isChatActive && keyboard.escapeKey.wasPressedThisFrame)
        {
            ToggleChat();
        }
    }

    private void OnChatInitialized()
    {
        // Mostrar chat automáticamente cuando se inicializa
        ShowChatWindow(true);

        // Mostrar mensaje de bienvenida
        DisplaySystemMessage("Bienvenido al chat del lobby. Usa /help para ver comandos disponibles.");
    }

    public void ShowChatWindow(bool show)
    {
        isChatActive = show;
        chatWindow.SetActive(show);

        if (show)
        {
            messageInput.Select();
            messageInput.ActivateInputField();
            StartCoroutine(ScrollToBottom());
        }
    }

    public void ToggleChat()
    {
        isChatActive = !isChatActive;
        chatWindow.SetActive(isChatActive);

        if (isChatActive)
        {
            messageInput.Select();
            messageInput.ActivateInputField();
            StartCoroutine(ScrollToBottom());
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void DisplayMessage(ChatMessage message)
    {
        GameObject prefabToUse = GetMessagePrefab(message);

        if (prefabToUse == null) return;

        GameObject messageObj = Instantiate(prefabToUse, messageContainer);

        // Configurar el texto del mensaje
        TMP_Text textComponent = messageObj.GetComponentInChildren<TMP_Text>();

        if (textComponent != null)
        {
            string formattedMessage = FormatMessage(message);
            textComponent.text = formattedMessage;
            textComponent.color = GetMessageColor(message);
        }

        messageInstances.Add(messageObj);

        // Limitar número de mensajes visibles
        if (messageInstances.Count > maxVisibleMessages)
        {
            Destroy(messageInstances[0]);
            messageInstances.RemoveAt(0);
        }

        // Auto-scroll si el chat está activo
        if (isChatActive)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    public void DisplaySystemMessage(string message)
    {
        var systemMessage = new ChatMessage
        {
            SenderDisplayName = "Sistema",
            MessageText = message,
            IsSystemMessage = true
        };

        DisplayMessage(systemMessage);
    }

    private GameObject GetMessagePrefab(ChatMessage message)
    {
        if (message.IsSystemMessage)
            return systemMessagePrefab;
        else if (message.IsDirectMessage)
            return privateMessagePrefab;
        else
            return messagePrefab;
    }

    private string FormatMessage(ChatMessage message)
    {
        if (message.IsSystemMessage)
            return $"[{message.SenderDisplayName}] {message.MessageText}";

        if (message.IsDirectMessage)
        {
            string currentPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;

            if (message.SenderPlayerId == currentPlayerId)
            {
                // Mensaje privado enviado por mí
                return $"<color=#{ColorUtility.ToHtmlStringRGB(privateMessageColor)}>[Privado a {message.RecipientDisplayName}]</color> {message.MessageText}";
            }
            else
            {
                // Mensaje privado recibido
                return $"<color=#{ColorUtility.ToHtmlStringRGB(privateMessageColor)}>[Privado de {message.SenderDisplayName}]</color> {message.MessageText}";
            }
        }

        // Mensaje público normal
        string playerColor = GetPlayerColorHex(message.SenderDisplayName);
        return $"<color={playerColor}>[{message.SenderDisplayName}]</color>: {message.MessageText}";
    }

    private Color GetMessageColor(ChatMessage message)
    {
        if (message.IsSystemMessage)
            return systemMessageColor;
        else if (message.IsDirectMessage)
            return privateMessageColor;
        else
            return normalMessageColor;
    }

    private string GetPlayerColorHex(string playerName)
    {
        if (!playerColors.ContainsKey(playerName))
        {
            // Generar color único basado en el nombre
            System.Random rand = new System.Random(playerName.GetHashCode());
            Color playerColor = Color.HSVToRGB(
                (float)rand.NextDouble(),
                0.7f + (float)rand.NextDouble() * 0.3f,
                0.8f + (float)rand.NextDouble() * 0.2f
            );
            playerColors[playerName] = playerColor;
        }

        return "#" + ColorUtility.ToHtmlStringRGB(playerColors[playerName]);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Doble frame para asegurar
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void OnSendClicked()
    {
        string message = messageInput.text.Trim();

        if (!string.IsNullOrEmpty(message))
        {
            // Procesar comandos
            if (message.StartsWith("/"))
            {
                ProcessCommand(message);
            }
            else
            {
                // Enviar mensaje normal
                ChatManager.Instance.SendMessage(message);
            }

            messageInput.text = "";

            if (isChatActive)
            {
                messageInput.Select();
                messageInput.ActivateInputField();
            }
        }
    }

    private void OnMessageSubmit(string text)
    {
        // Solo enviar si no estamos manteniendo Shift para multilínea
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            OnSendClicked();
        }
    }

    private void OnInputSelected(string text)
    {
        isTyping = true;
    }

    private void OnInputDeselected(string text)
    {
        isTyping = false;
    }

    private void ProcessCommand(string command)
    {
        command = command.Trim().ToLower();

        if (command.StartsWith("/w ") || command.StartsWith("/whisper ") || command.StartsWith("/msg "))
        {
            ProcessWhisperCommand(command);
        }
        else if (command == "/players" || command == "/who")
        {
            DisplayOnlinePlayers();
        }
        else if (command == "/help" || command == "/?")
        {
            DisplayHelp();
        }
        else if (command == "/clear")
        {
            ClearMessages();
        }
        else if (command == "/emoji")
        {
            DisplaySystemMessage("Emojis disponibles: :) :( :D :P ;)");
        }
        else
        {
            DisplaySystemMessage($"Comando desconocido: {command}. Usa /help para ver comandos disponibles.");
        }
    }

    private void ProcessWhisperCommand(string command)
    {
        // Extraer nombre y mensaje
        string[] parts = command.Split(new char[] { ' ' }, 3);

        if (parts.Length < 3)
        {
            DisplaySystemMessage("Uso: /w [nombre] [mensaje]");
            return;
        }

        string targetName = parts[1];
        string message = parts[2];

        // Buscar jugador por nombre
        var onlinePlayers = ChatManager.Instance.GetOnlinePlayers();
        string targetId = null;

        foreach (var player in onlinePlayers)
        {
            if (player.Value.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                targetId = player.Key;
                break;
            }
        }

        if (!string.IsNullOrEmpty(targetId))
        {
            ChatManager.Instance.SendMessage(message, targetId);
        }
        else
        {
            DisplaySystemMessage($"Jugador '{targetName}' no encontrado");
        }
    }

    private void DisplayOnlinePlayers()
    {
        var players = ChatManager.Instance.GetOnlinePlayers();

        if (players.Count == 0)
        {
            DisplaySystemMessage("No hay jugadores en el lobby");
            return;
        }

        string playerList = "Jugadores en el lobby:\n";
        int count = 1;

        foreach (var player in players)
        {
            playerList += $"{count}. {player.Value}\n";
            count++;
        }

        DisplaySystemMessage(playerList.Trim());
    }

    private void DisplayHelp()
    {
        string helpText = "Comandos disponibles:\n" +
                         "/w [nombre] [mensaje] - Mensaje privado\n" +
                         "/players - Lista de jugadores online\n" +
                         "/clear - Limpiar chat\n" +
                         "/emoji - Mostrar emojis disponibles\n" +
                         "/help - Muestra esta ayuda\n\n" +
                         "Teclas rápidas:\n" +
                         "T - Mostrar/ocultar chat\n" +
                         "Enter - Enviar mensaje\n" +
                         "Shift+Enter - Nueva línea\n" +
                         "Esc - Cerrar chat";

        DisplaySystemMessage(helpText);
    }

    public void ClearMessages()
    {
        foreach (var msg in messageInstances)
        {
            if (msg != null)
            {
                Destroy(msg);
            }
        }
        messageInstances.Clear();
    }
}