using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    // [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI volumeText;
    // [SerializeField] private TextMeshProUGUI muteSelfButtonText;
    // [SerializeField] private TextMeshProUGUI muteAllButtonText;

    [Header("Assets")]
    [SerializeField] private Texture mutedeSelfIcon;
    [SerializeField] private Texture unmutedSelfIcon;
    [SerializeField] private Texture mutedAllIcon;
    [SerializeField] private Texture unmutedAllIcon;
    private RawImage muteSelfButtonImage;
    private RawImage muteAllButtonImage;
    
    private string channelName;
    private VivoxManager vivoxManager;
    private bool isInitialized = false;

    private void Start()
    {
        vivoxManager = VivoxManager.Instance;
        
        if (vivoxManager == null)
        {
            Debug.LogError("VivoxManager non trouvé! Assurez-vous qu'il existe dans la scène.");
            return;
        }
        
        isInitialized = true;

        muteSelfButtonImage = muteSelfButton.GetComponent<RawImage>();
        muteAllButtonImage = muteAllButton.GetComponent<RawImage>();
        
        // S'abonner aux événements
        vivoxManager.OnLoginStatusChanged += OnLoginStatusChanged;
        vivoxManager.OnChannelConnectionChanged += OnChannelConnectionChanged;
        vivoxManager.OnErrorOccurred += OnError;
        vivoxManager.OnParticipantAdded += OnParticipantAdded;
        vivoxManager.OnParticipantRemoved += OnParticipantRemoved;
        
        SetupButtons();
        
        vivoxManager.Login();

        if (autoConnect)
        {
            Invoke(nameof(AutoConnectToVoiceChat), 2f);
        }
    }

    private void OnDestroy()
    {
        if (vivoxManager != null)
        {
            vivoxManager.OnLoginStatusChanged -= OnLoginStatusChanged;
            vivoxManager.OnChannelConnectionChanged -= OnChannelConnectionChanged;
            vivoxManager.OnErrorOccurred -= OnError;
            vivoxManager.OnParticipantAdded -= OnParticipantAdded;
            vivoxManager.OnParticipantRemoved -= OnParticipantRemoved;
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateUI();
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
        if (volumeText != null)
        {
            volumeText.text = (vivoxManager.GetSpeakerVolume() + 50).ToString()  + '%';
        }

        bool inChannel = vivoxManager.IsInChannel;
        if (muteSelfButton != null)
            muteSelfButton.interactable = inChannel;
        if (muteAllButton != null)
            muteAllButton.interactable = inChannel;
        if (increaseVolumeButton != null)
            increaseVolumeButton.interactable = inChannel;
        if (decreaseVolumeButton != null)
            decreaseVolumeButton.interactable = inChannel;
    }



    [ContextMenu("Connect to Voice Chat")]
    public void ConnectToVoiceChat()
    {
        if (vivoxManager == null) return;
        
        Debug.Log("Connecting to voice chat...");
        vivoxManager.Login();
        Invoke(nameof(JoinChannel), 1.5f);
    }

    [ContextMenu("Disconnect from Voice Chat")]
    public void DisconnectFromVoiceChat()
    {
        if (vivoxManager == null) return;
        
        Debug.Log("Disconnecting from voice chat...");
        vivoxManager.LeaveChannel();
        vivoxManager.Logout();
    }

    [ContextMenu("Toggle Mute Self")]
    public void ToggleMuteSelf()
    {
        if (vivoxManager == null || !vivoxManager.IsInChannel) return;
        
        vivoxManager.ToggleMuteSelf();
        muteSelfButtonImage.texture = vivoxManager.IsSelfMuted() ? mutedeSelfIcon : unmutedSelfIcon;
    }

    [ContextMenu("Toggle Mute All")]
    public void ToggleMuteAll()
    {
        if (vivoxManager == null || !vivoxManager.IsInChannel) return;
        
        vivoxManager.ToggleMuteAll();
        muteAllButtonImage.texture = vivoxManager.IsAllMuted() ? mutedAllIcon : unmutedAllIcon;
    }

    public void AdjustVolume(int delta)
    {
        if (vivoxManager == null || !vivoxManager.IsInChannel) return;
        
        int currentVolume = vivoxManager.GetSpeakerVolume();
        int newVolume = Mathf.Clamp(currentVolume + delta, -50, 50);
        vivoxManager.SetSpeakerVolume(newVolume);
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

    private void JoinChannel()
    {
        if (vivoxManager != null && vivoxManager.IsLoggedIn)
        {
            vivoxManager.JoinChannel(channelName);
        }
    }

    // Gestionnaires d'événements
    private void OnLoginStatusChanged(bool isLoggedIn)
    {
        Debug.Log($"<color=cyan>Login Status Changed: {isLoggedIn}</color>");
    }

    private void OnChannelConnectionChanged(bool isConnected)
    {
        Debug.Log($"<color=cyan>Channel Connection Changed: {isConnected}</color>");
    }

    private void OnError(string error)
    {
        Debug.LogError($"<color=red>Vivox Error: {error}</color>");
    }

    private void OnParticipantAdded(string participantName)
    {
        Debug.Log($"<color=green>Participant Joined: {participantName}</color>");
    }

    private void OnParticipantRemoved(string participantName)
    {
        Debug.Log($"<color=yellow>Participant Left: {participantName}</color>");
    }
}
