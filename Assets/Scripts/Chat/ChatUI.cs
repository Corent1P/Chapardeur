using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform chatContentContainer;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private GameObject chatWindow;
    [SerializeField] private KeyCode toggleChatKey = KeyCode.T;
    [SerializeField] private KeyCode sendMessageKey = KeyCode.Return;

    [Header("Settings")]
    [SerializeField] private bool showChatByDefault = false;
    [SerializeField] private int maxMessages = 100;

    private List<GameObject> _spawnedChatMessages = new List<GameObject>();
    private bool _isChatActive = false;
    private Dictionary<string, Color> playerNameColors = new Dictionary<string, Color>();

    void Start()
    {
        _isChatActive = showChatByDefault;
        chatWindow.SetActive(_isChatActive);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendButtonClicked);

        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnInputSubmit);
            chatInputField.onEndEdit.AddListener(OnInputEndEdit);
        }

        // Suscribirse a eventos del ChatManager
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived += DisplayNewMessage;
        }
    }

    void OnDestroy()
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived -= DisplayNewMessage;
        }

        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendButtonClicked);

        if (chatInputField != null)
        {
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);
            chatInputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        ClearChatMessages();
    }

    void Update()
    {
        // Toggle del chat con tecla
        if (Input.GetKeyDown(toggleChatKey))
        {
            ToggleChat();
        }

        // Enviar mensaje con Enter
        if (_isChatActive && Input.GetKeyDown(sendMessageKey) && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            OnSendButtonClicked();
        }
    }

    public void ToggleChat()
    {
        _isChatActive = !_isChatActive;
        chatWindow.SetActive(_isChatActive);

        if (_isChatActive)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    private void OnSendButtonClicked()
    {
        SendMessageFromInput();
    }

    private void OnInputSubmit(string input)
    {
        SendMessageFromInput();
    }

    private void OnInputEndEdit(string input)
    {
        if (Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            SendMessageFromInput();
        }
    }

    private void SendMessageFromInput()
    {
        string message = chatInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            chatInputField.text = "";
            return;
        }

        // Procesar comandos
        if (message.StartsWith("/w ") || message.StartsWith("/whisper ") || message.StartsWith("/msg "))
        {
            ProcessWhisperCommand(message);
        }
        else if (message.StartsWith("/players"))
        {
            DisplayOnlinePlayers();
        }
        else if (message.StartsWith("/help"))
        {
            DisplayHelp();
        }
        else if (message.StartsWith("/clear"))
        {
            ClearChatMessages();
        }
        else
        {
            // Enviar mensaje normal
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.SendMessage(message);
            }
        }

        chatInputField.text = "";

        if (_isChatActive)
        {
            chatInputField.ActivateInputField();
        }
    }

    private void ProcessWhisperCommand(string message)
    {
        string command = message.Substring(message.IndexOf(' ') + 1);
        string[] parts = command.Split(new char[] { ' ' }, 2);

        if (parts.Length < 2)
        {
            DisplaySystemMessage("Uso: /w [nombre] [mensaje]");
            return;
        }

        string targetName = parts[0];
        string messageContent = parts[1];

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
            ChatManager.Instance.SendMessage(messageContent, targetId);
        }
        else
        {
            DisplaySystemMessage($"Jugador '{targetName}' no encontrado");
        }
    }

    private void DisplayOnlinePlayers()
    {
        var players = ChatManager.Instance.GetOnlinePlayers();
        string playerList = "Jugadores online (" + players.Count + "):\n";

        foreach (var player in players)
        {
            playerList += $"- {player.Value}\n";
        }

        DisplaySystemMessage(playerList);
    }

    private void DisplayHelp()
    {
        string helpText = "Comandos disponibles:\n" +
                         "/w [nombre] [mensaje] - Mensaje privado\n" +
                         "/players - Lista de jugadores online\n" +
                         "/clear - Limpiar chat\n" +
                         "/help - Muestra esta ayuda";

        DisplaySystemMessage(helpText);
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

    public void DisplayNewMessage(ChatMessage message)
    {
        if (chatContentContainer == null || chatMessagePrefab == null) return;

        string formattedMessage;
        Color messageColor = Color.white;

        if (message.IsDirectMessage)
        {
            if (message.SenderDisplayName == "Tú" || message.SenderDisplayName == "You")
            {
                formattedMessage = $"<color=#FF9900>[Privado para {message.RecipientDisplayName}]</color> {message.MessageText}";
                messageColor = new Color(1f, 0.6f, 0f); // Naranja
            }
            else
            {
                formattedMessage = $"<color=#FF9900>[Privado de {message.SenderDisplayName}]</color> {message.MessageText}";
                messageColor = new Color(1f, 0.6f, 0f); // Naranja
            }
        }
        else
        {
            if (message.SenderDisplayName == "System")
            {
                formattedMessage = $"<color=#00CCFF>[Sistema]</color> {message.MessageText}";
                messageColor = new Color(0f, 0.8f, 1f); // Azul claro
            }
            else
            {
                // Asignar color único por jugador
                if (!playerNameColors.ContainsKey(message.SenderDisplayName))
                {
                    playerNameColors[message.SenderDisplayName] = GetRandomColorForPlayer(message.SenderDisplayName);
                }

                Color playerColor = playerNameColors[message.SenderDisplayName];
                string hexColor = ColorUtility.ToHtmlStringRGB(playerColor);

                formattedMessage = $"<color=#{hexColor}>[{message.SenderDisplayName}]</color> {message.MessageText}";
                messageColor = playerColor;
            }
        }

        GameObject messageGO = Instantiate(chatMessagePrefab, chatContentContainer);
        TextMeshProUGUI messageText = messageGO.GetComponent<TextMeshProUGUI>();

        if (messageText != null)
        {
            messageText.text = formattedMessage;
            messageText.color = messageColor;

            // Ajustar tamaño si es necesario
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
        }

        _spawnedChatMessages.Add(messageGO);

        // Limitar mensajes mostrados
        if (_spawnedChatMessages.Count > maxMessages)
        {
            Destroy(_spawnedChatMessages[0]);
            _spawnedChatMessages.RemoveAt(0);
        }

        StartCoroutine(ForceScrollDown());
    }

    private Color GetRandomColorForPlayer(string playerName)
    {
        // Generar color basado en hash del nombre para consistencia
        int hash = playerName.GetHashCode();
        System.Random rng = new System.Random(hash);

        // Evitar colores muy oscuros o muy claros
        float r = 0.3f + (float)rng.NextDouble() * 0.7f;
        float g = 0.3f + (float)rng.NextDouble() * 0.7f;
        float b = 0.3f + (float)rng.NextDouble() * 0.7f;

        return new Color(r, g, b);
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
            if (msg != null)
            {
                Destroy(msg);
            }
        }
        _spawnedChatMessages.Clear();

        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.ClearChat();
        }
    }
}