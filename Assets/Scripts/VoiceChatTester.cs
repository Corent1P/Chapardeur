using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script de test simple pour le chat vocal Vivox
/// Permet de tester rapidement les fonctionnalités sans UI complète
/// </summary>
public class VoiceChatTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private string testChannelName = "TestChannel";
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
    [SerializeField] private TextMeshProUGUI muteSelfButtonText;
    [SerializeField] private TextMeshProUGUI muteAllButtonText;
    
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
        
        // S'abonner aux événements
        vivoxManager.OnLoginStatusChanged += OnLoginStatusChanged;
        vivoxManager.OnChannelConnectionChanged += OnChannelConnectionChanged;
        vivoxManager.OnErrorOccurred += OnError;
        vivoxManager.OnParticipantAdded += OnParticipantAdded;
        vivoxManager.OnParticipantRemoved += OnParticipantRemoved;
        
        Debug.Log("=== VoiceChat Tester Initialized ===");
        
        // Configurer les boutons
        SetupButtons();
        
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
        
        // Mettre à jour l'UI
        UpdateUI();
    }
    
    private void SetupButtons()
    {
        // Configurer les listeners des boutons
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
        // Mettre à jour le texte de statut
        if (statusText != null)
        {
            statusText.text = $"Logged In: {(vivoxManager.IsLoggedIn ? "✓" : "✗")}\n" +
                             $"In Channel: {(vivoxManager.IsInChannel ? "✓" : "✗")}";
        }
        
        // Mettre à jour le texte de volume
        if (volumeText != null)
        {
            volumeText.text = $"Speaker: {vivoxManager.GetSpeakerVolume()}%\n" +
                             $"Mic: {vivoxManager.GetMicrophoneVolume()}%";
        }
        
        // Mettre à jour les textes des boutons mute
        if (muteSelfButtonText != null && vivoxManager.IsInChannel)
        {
            muteSelfButtonText.text = vivoxManager.IsSelfMuted() ? "Unmute Self" : "Mute Self";
        }
        
        if (muteAllButtonText != null && vivoxManager.IsInChannel)
        {
            muteAllButtonText.text = vivoxManager.IsAllMuted() ? "Unmute All" : "Mute All";
        }
        
        // Activer/désactiver les boutons selon l'état
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



    // Méthodes de test
    [ContextMenu("Connect to Voice Chat")]
    public void ConnectToVoiceChat()
    {
        if (vivoxManager == null) return;
        
        Debug.Log("Connecting to voice chat...");
        vivoxManager.Login();
        Invoke(nameof(JoinTestChannel), 1.5f);
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
        Debug.Log($"Mute Self: {vivoxManager.IsSelfMuted()}");
    }

    [ContextMenu("Toggle Mute All")]
    public void ToggleMuteAll()
    {
        if (vivoxManager == null || !vivoxManager.IsInChannel) return;
        
        vivoxManager.ToggleMuteAll();
        Debug.Log($"Mute All: {vivoxManager.IsAllMuted()}");
    }

    public void AdjustVolume(int delta)
    {
        if (vivoxManager == null || !vivoxManager.IsInChannel) return;
        
        int currentVolume = vivoxManager.GetSpeakerVolume();
        int newVolume = Mathf.Clamp(currentVolume + delta, 0, 100);
        vivoxManager.SetSpeakerVolume(newVolume);
        Debug.Log($"Speaker Volume: {newVolume}%");
    }

    private void AutoConnectToVoiceChat()
    {
        Debug.Log("Auto-connecting to voice chat...");
        ConnectToVoiceChat();
    }

    private void JoinTestChannel()
    {
        if (vivoxManager != null && vivoxManager.IsLoggedIn)
        {
            vivoxManager.JoinChannel(testChannelName);
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
