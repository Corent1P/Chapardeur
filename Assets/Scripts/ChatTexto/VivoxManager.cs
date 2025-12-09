using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core;

public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    public static event Action<ChatMessage> OnMessageReceivedUI;
    public static event Action OnVivoxInitialized;

    [SerializeField] private bool autoInitialize = true;

    private Dictionary<string, int> _savedVolumes = new Dictionary<string, int>();
    private Dictionary<string, bool> _savedMuteStates = new Dictionary<string, bool>();

    public bool IsMuted { get; private set; }
    public string CurrentVoiceChannel { get; private set; }
    public string CurrentTextChannel { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private LobbyManager _lobbyManager;

    private void Awake()
    {
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

        _lobbyManager = FindObjectOfType<LobbyManager>();
        if (_lobbyManager == null)
        {
            Debug.LogWarning("VivoxManager: LobbyManager not found. Chat may not work properly.");
        }

        await Task.Yield();

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"VivoxManager: Failed to initialize Unity Services: {ex.Message}");
                return;
            }
        }

        if (AuthenticationService.Instance.IsSignedIn)
        {
            await InitializeVivox();
        }
    }

    public async Task InitializeVivox()
    {
        if (VivoxService.Instance.IsLoggedIn)
        {
            Debug.Log("Vivox already logged in.");
            return;
        }

        try
        {
            string nickName = PlayerDataManager.Instance.PlayerName;
            LoginOptions loginOptions = new LoginOptions { DisplayName = nickName };

            // Setup event handlers
            VivoxService.Instance.LoggedIn += OnLoginSuccess;
            VivoxService.Instance.LoggedOut += OnLogout;
            VivoxService.Instance.ChannelJoined += OnChannelJoined;
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived += OnDirectMessageReceived;

            await VivoxService.Instance.LoginAsync(loginOptions);
            Debug.Log($"Vivox logged in successfully as: {nickName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox login failed: {ex.Message}");
        }
    }

    public async Task JoinLobbyChannel(string channelName)
    {
        if (!VivoxService.Instance.IsLoggedIn)
        {
            await InitializeVivox();
            if (!VivoxService.Instance.IsLoggedIn)
            {
                Debug.LogError("Vivox: Cannot join channel without being logged in.");
                return;
            }
        }

        try
        {
            CurrentVoiceChannel = channelName;
            CurrentTextChannel = channelName;

            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);
            Debug.Log($"Vivox: Joined channel {channelName} successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox: Failed to join channel {channelName}: {ex.Message}");
        }
    }

    public async Task LeaveAllChannelsAsync()
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        try
        {
            if (!string.IsNullOrEmpty(CurrentTextChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentTextChannel);
            }
            if (!string.IsNullOrEmpty(CurrentVoiceChannel))
            {
                await VivoxService.Instance.LeaveChannelAsync(CurrentVoiceChannel);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox: Error leaving channels: {ex.Message}");
        }
        finally
        {
            CurrentTextChannel = null;
            CurrentVoiceChannel = null;
            Debug.Log("Vivox: Left all channels.");
        }
    }

    public async Task SendMessageToChannel(string message, string channelName = null)
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        string targetChannel = channelName ?? CurrentTextChannel;
        if (string.IsNullOrEmpty(targetChannel))
        {
            Debug.LogError("Vivox: No channel to send message to.");
            return;
        }

        try
        {
            MessageOptions messageOptions = new MessageOptions();
            await VivoxService.Instance.SendChannelTextMessageAsync(targetChannel, message, messageOptions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox: Failed to send message: {ex.Message}");
        }
    }

    public async Task SendDirectMessage(string message, string targetPlayerId)
    {
        if (!VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(message)) return;

        try
        {
            // Get display name from player ID
            string displayName = GetPlayerDisplayName(targetPlayerId);
            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogError($"Vivox: Could not find player with ID {targetPlayerId}");
                return;
            }

            MessageOptions messageOptions = new MessageOptions();
            await VivoxService.Instance.SendDirectTextMessageAsync(displayName, message, messageOptions);

            // Also show the message locally
            var localMessage = new ChatMessage
            {
                SenderDisplayName = PlayerDataManager.Instance.PlayerName,
                RecipientDisplayName = displayName,
                MessageText = message,
                IsDirectMessage = true
            };
            OnMessageReceivedUI?.Invoke(localMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox: Failed to send direct message: {ex.Message}");
        }
    }

    private string GetPlayerDisplayName(string playerId)
    {
        // Try to get from lobby
        if (_lobbyManager != null && _lobbyManager.joinLobby != null)
        {
            var player = _lobbyManager.joinLobby.Players.Find(p => p.Id == playerId);
            if (player != null && player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                return player.Data["PlayerName"].Value;
            }
        }

        // Fallback to local player data manager
        if (playerId == AuthenticationService.Instance.PlayerId)
        {
            return PlayerDataManager.Instance.PlayerName;
        }

        return null;
    }

    private void OnLoginSuccess()
    {
        Debug.Log("Vivox: Login successful.");
        IsInitialized = true;
        OnVivoxInitialized?.Invoke();
    }

    private void OnLogout()
    {
        Debug.Log("Vivox: Logged out.");
        IsInitialized = false;
    }

    private void OnChannelJoined(string channelName)
    {
        Debug.Log($"Vivox: Joined channel {channelName}");
    }

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        var chatMessage = new ChatMessage
        {
            SenderDisplayName = message.SenderDisplayName,
            SenderPlayerId = message.SenderPlayerId,
            ChannelName = message.ChannelName,
            MessageText = message.MessageText,
            IsDirectMessage = false,
            RecipientDisplayName = null
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
            VivoxService.Instance.LoggedIn -= OnLoginSuccess;
            VivoxService.Instance.LoggedOut -= OnLogout;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            VivoxService.Instance.DirectedMessageReceived -= OnDirectMessageReceived;
        }
    }
}