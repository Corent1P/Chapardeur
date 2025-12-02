using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class LobbyChatManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button sendButton;

    private List<GameObject> messagePool = new List<GameObject>();
    private LobbyManager lobbyManager;

    void Start()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();

        if (chatPanel != null) chatPanel.SetActive(false);

        if (sendButton != null) sendButton.onClick.AddListener(SendMessage);
        if (chatInputField != null) chatInputField.onSubmit.AddListener((text) => SendMessage());

        // Escuchar eventos del lobby
        if (lobbyManager != null)
        {
            LobbyManager.OnPlayerJoinedLobby += OnPlayerJoined;
            LobbyManager.OnLobbyJoinedOrLeft += OnLobbyStateChanged;
        }
    }

    void Update()
    {
        // Toggle chat with Enter
       // if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))

       // {
        //    ToggleChat();
       // }s
    }

    void OnDestroy()
    {
        if (lobbyManager != null)
        {
            LobbyManager.OnPlayerJoinedLobby -= OnPlayerJoined;
            LobbyManager.OnLobbyJoinedOrLeft -= OnLobbyStateChanged;
        }
    }

    private void OnLobbyStateChanged()
    {
        if (lobbyManager.joinLobby == null)
        {
            ClearMessages();
            if (chatPanel != null)
            {
                chatPanel.SetActive(false); // Desactivar al salir
                Debug.Log("Chat DESACTIVADO - Sin lobby");
            }
        }
        else
        {
            // ✅ ACTIVAR CHAT al unirse/crear lobby
            if (chatPanel != null)
            {
                chatPanel.SetActive(true);
                Debug.Log("Chat ACTIVADO - Lobby unido");
            }

            SendSystemMessage($"Te has unido al lobby: {lobbyManager.joinLobby.Name}");
        }
    }

    private void OnPlayerJoined(Player player)
    {
        if (player.Data != null && player.Data.ContainsKey("PlayerName"))
        {
            string playerName = player.Data["PlayerName"].Value;
            if (playerName != PlayerDataManager.Instance.PlayerName) // No mostrar tu propia entrada
            {
                SendSystemMessage($"{playerName} se ha unido al lobby");
            }
        }
    }

    public void ToggleChat()
    {
        if (chatPanel == null) return;

        bool isActive = !chatPanel.activeSelf;
        chatPanel.SetActive(isActive);

        if (isActive)
        {
            chatInputField.Select();
            chatInputField.ActivateInputField();
        }
    }

    public void SendMessage()
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        chatInputField.text = "";

        if (message.StartsWith("/sendto "))
        {
            HandlePrivateMessage(message);
        }
        else
        {
            HandlePublicMessage(message);
        }
    }

    private void HandlePublicMessage(string message)
    {
        string playerName = PlayerDataManager.Instance.PlayerName;

        // Solo mostrar localmente (sin red por ahora)
        DisplayMessage(playerName, message, false, "");

        Debug.Log($"Chat: {playerName}: {message}");
    }

    private void HandlePrivateMessage(string message)
    {
        string command = message.Substring("/sendto ".Length);
        int firstQuote = command.IndexOf('"');
        int secondQuote = command.IndexOf('"', firstQuote + 1);

        if (firstQuote != -1 && secondQuote != -1)
        {
            string targetName = command.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
            string messageContent = command.Substring(secondQuote + 1).Trim();

            if (string.IsNullOrWhiteSpace(messageContent)) return;

            string localPlayerName = PlayerDataManager.Instance.PlayerName;

            // Verificar si el jugador existe
            bool playerExists = false;
            if (lobbyManager != null && lobbyManager.joinLobby != null)
            {
                foreach (var player in lobbyManager.joinLobby.Players)
                {
                    if (player.Data != null && player.Data.ContainsKey("PlayerName"))
                    {
                        if (player.Data["PlayerName"].Value == targetName)
                        {
                            playerExists = true;
                            break;
                        }
                    }
                }
            }

            if (playerExists)
            {
                DisplayMessage(localPlayerName, messageContent, true, targetName);
                Debug.Log($"Privado a {targetName}: {messageContent}");
            }
            else
            {
                SendSystemMessage($"No se encontró al jugador '{targetName}'");
            }
        }
        else
        {
            SendSystemMessage("Formato: /sendto \"Nombre\" mensaje");
        }
    }

    public void SendSystemMessage(string message)
    {
        DisplayMessage("System", message, false, "");
    }

    private void DisplayMessage(string sender, string message, bool isPrivate, string recipient)
    {
        if (messagePrefab == null || messageContainer == null) return;

        string formattedMessage = FormatMessage(sender, message, isPrivate, recipient);

        GameObject messageGO = Instantiate(messagePrefab, messageContainer);
        TMP_Text textComponent = messageGO.GetComponent<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = formattedMessage;
        }

        messagePool.Add(messageGO);

        // Limitar historial
        if (messagePool.Count > 50)
        {
            Destroy(messagePool[0]);
            messagePool.RemoveAt(0);
        }

        // Scroll al final
        StartCoroutine(ScrollToBottom());
    }

    private string FormatMessage(string sender, string message, bool isPrivate, string recipient)
    {
        string localPlayerName = PlayerDataManager.Instance.PlayerName;

        if (isPrivate)
        {
            if (sender == localPlayerName)
            {
                return $"<color=#FF6B9D>[Privado para {recipient}]:</color> {message}";
            }
            else
            {
                return $"<color=#6BCEFF>[Privado de {sender}]:</color> {message}";
            }
        }
        else
        {
            if (sender == "System")
            {
                return $"<color=#FFA500>[Sistema]: {message}</color>";
            }
            else
            {
                return $"<color=#4ECDC4>{sender}:</color> {message}";
            }
        }
    }


    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ClearMessages()
    {
        foreach (var msg in messagePool)
        {
            if (msg != null) Destroy(msg);
        }
        messagePool.Clear();
    }
}