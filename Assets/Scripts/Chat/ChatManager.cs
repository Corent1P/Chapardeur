using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [SerializeField] private ChatUIManager chatUIManager;
    [SerializeField] private float chatUpdateFrequency = 1.5f;

    private LobbyManager lobbyManager;
    private float chatUpdateTimer;
    private Dictionary<string, string> playerNamesCache = new Dictionary<string, string>();
    private Dictionary<string, DataObject> localLobbyData = new Dictionary<string, DataObject>();
    private List<string> processedMessageIds = new List<string>();

    // Constantes para claves de datos
    private const string KEY_CHAT_DATA = "ChatData";
    private const string KEY_CHAT_ENABLED = "ChatEnabled";

    public event Action<ChatMessage> OnMessageReceived;
    public event Action OnChatInitialized;
    public event Action OnChatCleared;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();

        // Iniciar polling solo si tenemos lobby
        if (lobbyManager != null && lobbyManager.joinLobby != null)
        {
            StartPolling();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        StopAllCoroutines();
    }

    private void StartPolling()
    {
        // Usar InvokeRepeating para polling constante
        InvokeRepeating(nameof(PollLobbyForMessages), 1f, chatUpdateFrequency);
    }

    private void StopPolling()
    {
        CancelInvoke(nameof(PollLobbyForMessages));
    }

    private async void PollLobbyForMessages()
    {
        Debug.Log($"=== CHAT POLLING: Iniciando polling...");

        if (lobbyManager == null || lobbyManager.joinLobby == null)
        {
            Debug.LogWarning("=== CHAT: No hay lobby activo, deteniendo polling");
            StopPolling();
            return;
        }

        try
        {
            Debug.Log($"=== CHAT: Obteniendo lobby actualizado...");
            var updatedLobby = await LobbyService.Instance.GetLobbyAsync(lobbyManager.joinLobby.Id);

            Debug.Log($"=== CHAT: Lobby obtenido. Tiene datos? {updatedLobby.Data != null}");

            if (updatedLobby.Data != null && updatedLobby.Data.ContainsKey(KEY_CHAT_DATA))
            {
                string chatData = updatedLobby.Data[KEY_CHAT_DATA].Value;
                Debug.Log($"=== CHAT: Datos del chat: '{chatData}'");

                string currentChatData = localLobbyData.ContainsKey(KEY_CHAT_DATA) ?
                    localLobbyData[KEY_CHAT_DATA].Value : "";

                Debug.Log($"=== CHAT: Datos actuales vs nuevos: '{currentChatData}' vs '{chatData}'");

                if (chatData != currentChatData)
                {
                    Debug.Log($"=== CHAT: ¡Nuevos mensajes detectados! Procesando...");
                    ProcessChatData(chatData);
                    localLobbyData[KEY_CHAT_DATA] = updatedLobby.Data[KEY_CHAT_DATA];
                }
            }
            else
            {
                Debug.LogWarning("=== CHAT: No hay KEY_CHAT_DATA en el lobby");
            }
        }
        catch (Exception e)
        {

        }
    }

    private void UpdatePlayerNamesCache(Lobby lobby)
    {
        if (lobby.Players == null) return;

        foreach (var player in lobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                string playerName = player.Data["PlayerName"].Value;
                string playerId = player.Id;

                if (!playerNamesCache.ContainsKey(playerId))
                {
                    playerNamesCache[playerId] = playerName;
                }
                else if (playerNamesCache[playerId] != playerName)
                {
                    playerNamesCache[playerId] = playerName;
                }
            }
        }
    }

    private void ProcessChatData(string chatData)
    {
        if (string.IsNullOrEmpty(chatData)) return;

        // Formato: messageId|senderId|senderName|message|timestamp|isDirect|recipientId|recipientName
        string[] messages = chatData.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (string message in messages)
        {
            if (string.IsNullOrEmpty(message)) continue;

            string[] parts = message.Split('|');
            if (parts.Length >= 6)
            {
                string messageId = parts[0];
                string senderId = parts[1];
                string senderName = parts[2];
                string messageText = parts[3];
                string timestamp = parts[4];
                bool isDirect = bool.TryParse(parts[5], out bool direct) && direct;
                string recipientId = parts.Length > 6 ? parts[6] : "";
                string recipientName = parts.Length > 7 ? parts[7] : "";

                if (!processedMessageIds.Contains(messageId))
                {
                    var chatMessage = new ChatMessage
                    {
                        MessageId = messageId,
                        SenderPlayerId = senderId,
                        SenderDisplayName = senderName,
                        MessageText = messageText,
                        IsDirectMessage = isDirect,
                        RecipientPlayerId = recipientId,
                        RecipientDisplayName = recipientName,
                        Timestamp = DateTime.FromBinary(long.Parse(timestamp))
                    };

                    // Manejar límite de mensajes procesados
                    if (processedMessageIds.Count > 100)
                        processedMessageIds.RemoveAt(0);

                    processedMessageIds.Add(messageId);

                    // Solo mostrar mensajes públicos o privados dirigidos a mí
                    if (!isDirect || IsMessageForMe(chatMessage))
                    {
                        OnMessageReceived?.Invoke(chatMessage);

                        if (chatUIManager != null)
                        {
                            chatUIManager.DisplayMessage(chatMessage);
                        }
                    }
                }
            }
        }
    }

    private bool IsMessageForMe(ChatMessage message)
    {
        if (!message.IsDirectMessage) return true;

        string currentPlayerId = AuthenticationService.Instance.PlayerId;

        // El mensaje es para mí si soy el destinatario o el remitente
        return message.RecipientPlayerId == currentPlayerId ||
               message.SenderPlayerId == currentPlayerId;
    }

    public async void SendMessage(string message, string targetPlayerId = null)
    {
        Debug.Log($"=== CHAT: Intentando enviar mensaje: '{message}'");

        if (lobbyManager == null || lobbyManager.joinLobby == null)
        {
            Debug.LogError("=== CHAT ERROR: No hay lobbyManager o no estamos en un lobby");
            return;
        }

        Debug.Log($"=== CHAT: Lobby ID: {lobbyManager.joinLobby.Id}");
        Debug.Log($"=== CHAT: Player ID: {AuthenticationService.Instance.PlayerId}");

        if (string.IsNullOrWhiteSpace(message)) return;

        string playerId = AuthenticationService.Instance.PlayerId;
        string playerName = GetPlayerName(playerId);

        try
        {
            // Obtener chat actual del lobby
            string currentChatData = "";
            if (localLobbyData.ContainsKey(KEY_CHAT_DATA))
            {
                currentChatData = localLobbyData[KEY_CHAT_DATA].Value;
            }

            // Generar ID único para el mensaje
            string messageId = $"{playerId}_{DateTime.UtcNow.Ticks}";

            // Construir nuevo mensaje
            string newMessage = $"{messageId}|{playerId}|{playerName}|{message}|{DateTime.UtcNow.Ticks}|false";

            if (!string.IsNullOrEmpty(targetPlayerId))
            {
                string targetName = GetPlayerName(targetPlayerId);
                newMessage += $"|true|{targetPlayerId}|{targetName}";

                // Mostrar mensaje privado localmente inmediatamente
                var directMessage = new ChatMessage
                {
                    MessageId = messageId,
                    SenderPlayerId = playerId,
                    SenderDisplayName = playerName,
                    MessageText = message,
                    IsDirectMessage = true,
                    RecipientPlayerId = targetPlayerId,
                    RecipientDisplayName = targetName,
                    Timestamp = DateTime.UtcNow
                };

                if (chatUIManager != null)
                {
                    chatUIManager.DisplayMessage(directMessage);
                }
            }
            else
            {
                // Mostrar mensaje público localmente inmediatamente
                var publicMessage = new ChatMessage
                {
                    MessageId = messageId,
                    SenderPlayerId = playerId,
                    SenderDisplayName = playerName,
                    MessageText = message,
                    IsDirectMessage = false,
                    Timestamp = DateTime.UtcNow
                };

                if (chatUIManager != null)
                {
                    chatUIManager.DisplayMessage(publicMessage);
                }
            }

            // Agregar al chat existente
            List<string> messageList = new List<string>();
            if (!string.IsNullOrEmpty(currentChatData))
            {
                messageList.AddRange(currentChatData.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }

            messageList.Add(newMessage);

            // Limitar a 50 mensajes
            if (messageList.Count > 50)
            {
                messageList.RemoveAt(0);
            }

            string updatedChatData = string.Join(";", messageList);

            // Actualizar lobby en el servidor
            await LobbyService.Instance.UpdateLobbyAsync(lobbyManager.joinLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_CHAT_DATA, new DataObject(DataObject.VisibilityOptions.Member, updatedChatData) }
                }
            });

            // Actualizar datos locales
            localLobbyData[KEY_CHAT_DATA] = new DataObject(DataObject.VisibilityOptions.Member, updatedChatData);

            Debug.Log("Message sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }
    }

    public void SendSystemMessage(string message)
    {
        var systemMessage = new ChatMessage
        {
            MessageId = $"system_{DateTime.UtcNow.Ticks}",
            SenderPlayerId = "system",
            SenderDisplayName = "Sistema",
            MessageText = message,
            IsDirectMessage = false,
            IsSystemMessage = true,
            Timestamp = DateTime.UtcNow
        };

        OnMessageReceived?.Invoke(systemMessage);

        if (chatUIManager != null)
        {
            chatUIManager.DisplayMessage(systemMessage);
        }
    }

    public string GetPlayerName(string playerId)
    {
        // Buscar en caché
        if (playerNamesCache.TryGetValue(playerId, out string name))
            return name;

        // Buscar en el lobby actual
        if (lobbyManager != null && lobbyManager.joinLobby != null)
        {
            foreach (var player in lobbyManager.joinLobby.Players)
            {
                if (player.Id == playerId && player.Data != null && player.Data.ContainsKey("PlayerName"))
                {
                    name = player.Data["PlayerName"].Value;
                    playerNamesCache[playerId] = name;
                    return name;
                }
            }
        }

        // Si no se encuentra, usar ID truncado
        string shortId = playerId.Length > 4 ? playerId.Substring(0, 4) : playerId;
        return $"Player_{shortId}";
    }

    public Dictionary<string, string> GetOnlinePlayers()
    {
        var players = new Dictionary<string, string>();

        if (lobbyManager == null || lobbyManager.joinLobby == null) return players;

        foreach (var player in lobbyManager.joinLobby.Players)
        {
            string playerName = GetPlayerName(player.Id);
            players[player.Id] = playerName;
        }

        return players;
    }

    public void ClearChat()
    {
        processedMessageIds.Clear();
        playerNamesCache.Clear();
        localLobbyData.Clear();

        if (chatUIManager != null)
        {
            chatUIManager.ClearMessages();
        }

        OnChatCleared?.Invoke();
    }

    public void InitializeChatForLobby(Lobby lobby)
    {
        if (lobby == null) return;

        ClearChat();

        if (lobby.Data != null)
        {
            localLobbyData = new Dictionary<string, DataObject>(lobby.Data);
        }

        UpdatePlayerNamesCache(lobby);

        // Cargar mensajes existentes si los hay
        if (localLobbyData.ContainsKey(KEY_CHAT_DATA))
        {
            string chatData = localLobbyData[KEY_CHAT_DATA].Value;
            ProcessChatData(chatData);
        }

        // Iniciar polling
        StartPolling();

        OnChatInitialized?.Invoke();

        Debug.Log($"Chat initialized for lobby: {lobby.Name}");
    }

    public void CheckForLobbyUpdates(Lobby lobby)
    {
        if (lobby.Data != null && lobby.Data.ContainsKey(KEY_CHAT_DATA))
        {
            string newChatData = lobby.Data[KEY_CHAT_DATA].Value;
            string currentChatData = localLobbyData.ContainsKey(KEY_CHAT_DATA) ?
                localLobbyData[KEY_CHAT_DATA].Value : "";

            if (newChatData != currentChatData)
            {
                ProcessChatData(newChatData);
                localLobbyData[KEY_CHAT_DATA] = lobby.Data[KEY_CHAT_DATA];
            }
        }
    }
}