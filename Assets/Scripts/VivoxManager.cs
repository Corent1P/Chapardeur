using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }
    public static event Action<ChatMessage> OnMessageReceivedUI;
    public static event Action OnVivoxInitialized;

    [Header("Settings")]
    [SerializeField] private bool autoInitialize = true;

    // Estado
    public bool IsLoggedIn => VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn;
    public bool IsInitialized { get; private set; } = false;
    public string CurrentVoiceChannel { get; private set; }
    public string CurrentTextChannel { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        if (!autoInitialize) return;

        await Task.Delay(1000); // Esperar para evitar conflictos de inicialización

        try
        {
            // Inicializar Unity Services si no lo están
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            // Autenticación si no está logueado
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Inicializar Vivox
            await InitializeVivox();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing Vivox: {e.Message}");
        }
    }

    public async Task InitializeVivox()
    {
        if (IsLoggedIn) return;

        try
        {
            string displayName = AuthenticationService.Instance.PlayerName ?? $"Player_{UnityEngine.Random.Range(1000, 9999)}";

            var loginOptions = new LoginOptions
            {
                DisplayName = displayName
            };

            // Suscribir eventos ANTES del login
            VivoxService.Instance.LoggedIn += OnVivoxLoggedIn;
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived += OnDirectMessageReceived;
            VivoxService.Instance.ChannelJoined += OnChannelJoined;

            await VivoxService.Instance.LoginAsync(loginOptions);

            Debug.Log($"Vivox logged in as: {displayName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox login failed: {e.Message}");
        }
    }

    public async Task JoinLobbyChannel(string channelName)
    {
        if (!IsLoggedIn)
        {
            await InitializeVivox();
            if (!IsLoggedIn)
            {
                Debug.LogError("Cannot join channel without being logged in");
                return;
            }
        }

        try
        {
            // Salir del canal actual si existe
            if (!string.IsNullOrEmpty(CurrentTextChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentTextChannel);
            }

            CurrentVoiceChannel = channelName;
            CurrentTextChannel = channelName;

            // Unirse al canal con capacidades de texto y audio
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);

            Debug.Log($"Successfully joined channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join channel {channelName}: {e.Message}");
        }
    }

    public async Task LeaveAllChannelsAsync()
    {
        if (!IsLoggedIn) return;

        try
        {
            if (!string.IsNullOrEmpty(CurrentTextChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentTextChannel);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error leaving channels: {e.Message}");
        }
        finally
        {
            CurrentTextChannel = null;
            CurrentVoiceChannel = null;
            Debug.Log("Left all Vivox channels");
        }
    }

    public async Task SendMessageToChannel(string message, string channelName = null)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("Cannot send message: Not logged into Vivox");
            return;
        }

        string targetChannel = channelName ?? CurrentTextChannel;
        if (string.IsNullOrEmpty(targetChannel))
        {
            Debug.LogError("No channel to send message to");
            return;
        }

        try
        {
            MessageOptions messageOptions = new MessageOptions();
            await VivoxService.Instance.SendChannelTextMessageAsync(targetChannel, message, messageOptions);
            Debug.Log($"Message sent to {targetChannel}: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");
        }
    }

    public async Task SendDirectMessage(string message, string targetPlayerId)
    {
        if (!IsLoggedIn || string.IsNullOrEmpty(message)) return;

        try
        {
            // Obtener nombre del jugador desde el lobby
            string displayName = GetPlayerDisplayName(targetPlayerId);
            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogError($"Could not find player with ID {targetPlayerId}");
                return;
            }

            MessageOptions messageOptions = new MessageOptions();
            await VivoxService.Instance.SendDirectTextMessageAsync(displayName, message, messageOptions);

            // Mostrar el mensaje localmente también
            var localMessage = new ChatMessage
            {
                SenderDisplayName = AuthenticationService.Instance.PlayerName ?? "You",
                RecipientDisplayName = displayName,
                MessageText = message,
                IsDirectMessage = true
            };
            OnMessageReceivedUI?.Invoke(localMessage);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send direct message: {e.Message}");
        }
    }

    private string GetPlayerDisplayName(string playerId)
    {
        // Buscar en el LobbyManager
        var lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null && lobbyManager.joinLobby != null)
        {
            var player = lobbyManager.joinLobby.Players.Find(p => p.Id == playerId);
            if (player != null && player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                return player.Data["PlayerName"].Value;
            }
        }

        // Fallback
        if (playerId == AuthenticationService.Instance.PlayerId)
        {
            return AuthenticationService.Instance.PlayerName ?? "You";
        }

        return null;
    }

    private void OnVivoxLoggedIn()
    {
        IsInitialized = true;
        OnVivoxInitialized?.Invoke();
        Debug.Log("Vivox login successful");
    }

    private void OnChannelJoined(string channelName)
    {
        Debug.Log($"Joined channel: {channelName}");
    }

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        var chatMessage = new ChatMessage
        {
            SenderDisplayName = message.SenderDisplayName,
            SenderPlayerId = message.SenderPlayerId,
            ChannelName = message.ChannelName,
            MessageText = message.MessageText,
            IsDirectMessage = false
        };

        OnMessageReceivedUI?.Invoke(chatMessage);
    }

    private void OnDirectMessageReceived(VivoxMessage message)
    {
        var chatMessage = new ChatMessage
        {
            SenderDisplayName = message.SenderDisplayName,
            SenderPlayerId = message.SenderPlayerId,
            ChannelName = message.ChannelName,
            MessageText = message.MessageText,
            IsDirectMessage = true,
            RecipientDisplayName = message.RecipientPlayerId
        };

        OnMessageReceivedUI?.Invoke(chatMessage);
    }

    private void OnDestroy()
    {
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.LoggedIn -= OnVivoxLoggedIn;
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived -= OnDirectMessageReceived;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
        }
    }

    private void OnApplicationQuit()
    {
        _ = LeaveAllChannelsAsync();
    }
}