using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class VoiceChatLobby : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoConnect = true;

    [Header("UI Buttons")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button muteSelfButton;
    [SerializeField] private Button muteAllButton;
    [SerializeField] private Button increaseVolumeButton;
    [SerializeField] private Button decreaseVolumeButton;

    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI volumeText;
    [SerializeField] private TextMeshProUGUI participantsText;

    [Header("Assets")]
    [SerializeField] private Texture mutedSelfIcon;
    [SerializeField] private Texture unmutedSelfIcon;
    [SerializeField] private Texture mutedAllIcon;
    [SerializeField] private Texture unmutedAllIcon;
    private RawImage muteSelfButtonImage;
    private RawImage muteAllButtonImage;

    private string channelName;
    private VivoxManager vivoxManager;
    private bool isInitialized = false;
    private LobbyManager lobbyManager;

    private void Start()
    {
        vivoxManager = VivoxManager.Instance;
        lobbyManager = FindObjectOfType<LobbyManager>();

        if (vivoxManager == null)
        {
            Debug.LogError("VivoxManager not found!");
            return;
        }

        isInitialized = true;

        // Get RawImage components
        if (muteSelfButton != null)
            muteSelfButtonImage = muteSelfButton.GetComponent<RawImage>();
        if (muteAllButton != null)
            muteAllButtonImage = muteAllButton.GetComponent<RawImage>();

        SetupButtons();

        // Auto login
        _ = vivoxManager.InitializeVivox();

        if (autoConnect)
        {
            Invoke(nameof(AutoConnectToVoiceChat), 2f);
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        if (vivoxManager != null)
        {
            _ = vivoxManager.LeaveAllChannelsAsync();
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateUI();

        // Update channel name from lobby
        if (lobbyManager != null && lobbyManager.joinLobby != null)
        {
            UpdateChannelName(lobbyManager.joinLobby.Id);
        }
    }

    private void SetupButtons()
    {
        if (connectButton != null)
            connectButton.onClick.AddListener(ConnectToVoiceChat);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectFromVoiceChat);

        if (muteSelfButton != null)
            muteSelfButton.onClick.AddListener(ToggleMuteSelf);

        if (muteAllButton != null)
            muteAllButton.onClick.AddListener(ToggleMuteAll);

        if (increaseVolumeButton != null)
            increaseVolumeButton.onClick.AddListener(() => AdjustVolume(10));

        if (decreaseVolumeButton != null)
            decreaseVolumeButton.onClick.AddListener(() => AdjustVolume(-10));
    }

    private void UpdateUI()
    {
        // Update status text
        if (statusText != null)
        {
            if (!vivoxManager.IsInitialized)
                statusText.text = "Initializing...";
            else if (!vivoxManager.IsLoggedIn)
                statusText.text = "Not Logged In";
            else if (string.IsNullOrEmpty(vivoxManager.CurrentVoiceChannel))
                statusText.text = "Not Connected";
            else
                statusText.text = $"Connected to: {vivoxManager.CurrentVoiceChannel}";
        }

        // Update volume text
        if (volumeText != null)
        {
            if (vivoxManager.IsLoggedIn)
            {
                // Get volume from Vivox (simplified - you might need to track this separately)
                volumeText.text = "50%"; // Default value
            }
            else
            {
                volumeText.text = "N/A";
            }
        }

        // Update participants count
        if (participantsText != null)
        {
            if (vivoxManager.IsLoggedIn && !string.IsNullOrEmpty(vivoxManager.CurrentVoiceChannel))
            {
                // Note: Vivox v16+ doesn't have easy participant count in the service
                // You might need to track this manually or use lobby players count
                if (lobbyManager != null && lobbyManager.joinLobby != null)
                {
                    participantsText.text = $"Players: {lobbyManager.joinLobby.Players.Count}";
                }
                else
                {
                    participantsText.text = "Players: 1";
                }
            }
            else
            {
                participantsText.text = "Players: 0";
            }
        }

        // Update button states and icons
        bool inChannel = vivoxManager.IsLoggedIn && !string.IsNullOrEmpty(vivoxManager.CurrentVoiceChannel);

        if (muteSelfButton != null)
        {
            muteSelfButton.interactable = inChannel;
            if (muteSelfButtonImage != null)
            {
                // Simple icon toggle based on mute state (you'd need to track this)
                muteSelfButtonImage.texture = IsSelfMuted() ? mutedSelfIcon : unmutedSelfIcon;
            }
        }

        if (muteAllButton != null)
        {
            muteAllButton.interactable = inChannel;
            if (muteAllButtonImage != null)
            {
                muteAllButtonImage.texture = IsAllMuted() ? mutedAllIcon : unmutedAllIcon;
            }
        }

        if (increaseVolumeButton != null)
            increaseVolumeButton.interactable = inChannel;
        if (decreaseVolumeButton != null)
            decreaseVolumeButton.interactable = inChannel;
        if (connectButton != null)
            connectButton.interactable = !inChannel;
        if (disconnectButton != null)
            disconnectButton.interactable = inChannel;
    }

    public void ConnectToVoiceChat()
    {
        if (vivoxManager == null) return;

        Debug.Log("Connecting to voice chat...");

        // First ensure we're logged in
        if (!vivoxManager.IsLoggedIn)
        {
            _ = vivoxManager.InitializeVivox();
        }

        // Join channel if we have one
        if (!string.IsNullOrEmpty(channelName))
        {
            _ = vivoxManager.JoinLobbyChannel(channelName);
        }
        else if (lobbyManager != null && lobbyManager.joinLobby != null)
        {
            // Use lobby ID as channel
            _ = vivoxManager.JoinLobbyChannel(lobbyManager.joinLobby.Id);
        }
        else
        {
            Debug.LogWarning("No channel name set for voice chat");
        }
    }

    public void DisconnectFromVoiceChat()
    {
        if (vivoxManager == null) return;

        Debug.Log("Disconnecting from voice chat...");
        _ = vivoxManager.LeaveAllChannelsAsync();
    }

    public void ToggleMuteSelf()
    {
        if (vivoxManager == null || !vivoxManager.IsLoggedIn) return;

        // Note: Vivox v16+ doesn't have direct mute methods in the service
        // You would need to implement this using VivoxService.Instance
        // For now, we'll track it locally
        ToggleLocalMuteState();
        Debug.Log($"Self mute toggled: {IsSelfMuted()}");
    }

    public void ToggleMuteAll()
    {
        if (vivoxManager == null || !vivoxManager.IsLoggedIn) return;

        // Note: Same as above - need to implement with VivoxService.Instance
        ToggleLocalAllMuteState();
        Debug.Log($"Mute all toggled: {IsAllMuted()}");
    }

    public void AdjustVolume(int delta)
    {
        if (vivoxManager == null || !vivoxManager.IsLoggedIn) return;

        // Note: Volume control needs VivoxService.Instance
        // For now, just log
        Debug.Log($"Adjusting volume by: {delta}");
    }

    private void AutoConnectToVoiceChat()
    {
        Debug.Log("Auto-connecting to voice chat...");
        ConnectToVoiceChat();
    }

    public void UpdateChannelName(string name)
    {
        channelName = name;
    }

    // Simplified local state tracking (since Vivox v16+ API changed)
    private bool _isSelfMuted = false;
    private bool _isAllMuted = false;

    private void ToggleLocalMuteState()
    {
        _isSelfMuted = !_isSelfMuted;
    }

    private bool IsSelfMuted()
    {
        return _isSelfMuted;
    }

    private void ToggleLocalAllMuteState()
    {
        _isAllMuted = !_isAllMuted;
    }

    private bool IsAllMuted()
    {
        return _isAllMuted;
    }


}