using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

/// <summary>
/// Gestionnaire du chat audio Vivox pour le lobby/room d'attente (v16+)
/// </summary>
public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string channelName = "LobbyChannel";
    [SerializeField] private bool use3DAudio = false;
    
    [Header("Audio Settings (range -50 to 50)")]
    [SerializeField] private int defaultSpeakerVolume = 0;
    [SerializeField] private int defaultMicrophoneVolume = 0;
    
    // Events pour l'UI
    public event Action<bool> OnLoginStatusChanged;
    public event Action<bool> OnChannelConnectionChanged;
    public event Action<string> OnErrorOccurred;
    public event Action<string> OnParticipantAdded;
    public event Action<string> OnParticipantRemoved;
    
    private string _currentChannelName = null;
    private IVivoxService _vivoxService;
    
    // État
    public bool IsLoggedIn => _vivoxService != null && _vivoxService.IsLoggedIn;
    public bool IsInChannel => _vivoxService != null && _vivoxService.ActiveChannels != null && _vivoxService.ActiveChannels.Count > 0;
    
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
        try
        {
            // Initialiser Unity Services
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }
            
            // S'authentifier si nécessaire
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            // Récupérer le service Vivox
            _vivoxService = VivoxService.Instance;
            await _vivoxService.InitializeAsync();
            
            // S'abonner aux événements
            _vivoxService.LoggedIn += OnVivoxLoggedIn;
            _vivoxService.LoggedOut += OnVivoxLoggedOut;
            _vivoxService.ChannelJoined += OnVivoxChannelJoined;
            _vivoxService.ChannelLeft += OnVivoxChannelLeft;
            _vivoxService.ParticipantAddedToChannel += OnVivoxParticipantAdded;
            _vivoxService.ParticipantRemovedFromChannel += OnVivoxParticipantRemoved;
            
            Debug.Log("Vivox initialisé avec succès");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur lors de l'initialisation de Vivox: {e.Message}");
            OnErrorOccurred?.Invoke($"Erreur d'initialisation: {e.Message}");
        }
    }

    /// <summary>
    /// Se connecter au service Vivox
    /// </summary>
    public async void Login()
    {
        if (IsLoggedIn)
        {
            Debug.LogWarning("Déjà connecté à Vivox");
            return;
        }

        try
        {
            var displayName = AuthenticationService.Instance.PlayerName ?? $"Player_{UnityEngine.Random.Range(1000, 9999)}";
            
            var loginOptions = new LoginOptions
            {
                DisplayName = displayName
            };

            await _vivoxService.LoginAsync(loginOptions);
            
            Debug.Log($"Connecté à Vivox en tant que {displayName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur lors de la connexion à Vivox: {e.Message}");
            OnErrorOccurred?.Invoke($"Erreur de connexion: {e.Message}");
        }
    }

    /// <summary>
    /// Rejoindre un canal audio (lobby/room)
    /// </summary>
    public async void JoinChannel(string customChannelName = null)
    {
        if (!IsLoggedIn)
        {
            Debug.LogWarning("Vous devez être connecté avant de rejoindre un canal");
            Login();
            await Task.Delay(2000); // Attendre la connexion
        }

        if (IsInChannel)
        {
            Debug.LogWarning("Déjà dans un canal");
            return;
        }

        try
        {
            string targetChannelName = customChannelName ?? channelName;
            _currentChannelName = targetChannelName;
            
            if (use3DAudio)
            {
                // Canal 3D/positional
                var props = new Channel3DProperties(
                    audibleDistance: 32,
                    conversationalDistance: 1,
                    audioFadeIntensityByDistanceaudio: 1.0f,
                    audioFadeModel: AudioFadeModel.InverseByDistance
                );
                
                await _vivoxService.JoinPositionalChannelAsync(targetChannelName, ChatCapability.AudioOnly, props);
            }
            else
            {
                // Canal 2D standard
                await _vivoxService.JoinGroupChannelAsync(targetChannelName, ChatCapability.AudioOnly);
            }
            
            // Configurer les volumes par défaut
            SetSpeakerVolume(defaultSpeakerVolume);
            SetMicrophoneVolume(defaultMicrophoneVolume);
            
            Debug.Log($"Rejoint le canal: {targetChannelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur lors de la connexion au canal: {e.Message}");
            OnErrorOccurred?.Invoke($"Erreur de connexion au canal: {e.Message}");
        }
    }

    /// <summary>
    /// Quitter le canal audio actuel
    /// </summary>
    public async void LeaveChannel()
    {
        if (!IsInChannel || string.IsNullOrEmpty(_currentChannelName))
        {
            Debug.LogWarning("Pas dans un canal");
            return;
        }

        try
        {
            await _vivoxService.LeaveChannelAsync(_currentChannelName);
            _currentChannelName = null;
            Debug.Log("Canal quitté");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur en quittant le canal: {e.Message}");
        }
    }

    /// <summary>
    /// Se déconnecter de Vivox
    /// </summary>
    public void Logout()
    {
        if (IsLoggedIn)
        {
            LeaveChannel();
            _vivoxService.LogoutAsync();
            Debug.Log("Déconnecté de Vivox");
        }
    }

    /// <summary>
    /// Activer/Désactiver le microphone local
    /// </summary>
    public void ToggleMuteSelf()
    {
        if (!IsInChannel) return;
        
        try
        {
            bool currentMuteState = _vivoxService.IsInputDeviceMuted;
            if (currentMuteState)
            {
                _vivoxService.UnmuteInputDevice();
                Debug.Log("Microphone activé");
            }
            else
            {
                _vivoxService.MuteInputDevice();
                Debug.Log("Microphone coupé");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur toggle mute: {e.Message}");
        }
    }

    /// <summary>
    /// Définir l'état mute du microphone
    /// </summary>
    public void SetMuteSelf(bool muted)
    {
        if (!IsInChannel) return;
        
        if (muted)
            _vivoxService.MuteInputDevice();
        else
            _vivoxService.UnmuteInputDevice();
    }

    /// <summary>
    /// Obtenir l'état mute du microphone
    /// </summary>
    public bool IsSelfMuted()
    {
        return _vivoxService?.IsInputDeviceMuted ?? false;
    }

    /// <summary>
    /// Activer/Désactiver tous les haut-parleurs (autres joueurs)
    /// </summary>
    public void ToggleMuteAll()
    {
        if (!IsInChannel || _vivoxService.ActiveChannels == null) return;
        
        try
        {
            bool currentState = _vivoxService.IsOutputDeviceMuted;
            if (currentState)
            {
                _vivoxService.UnmuteOutputDevice();
                Debug.Log("Tous les joueurs unmute");
            }
            else
            {
                _vivoxService.MuteOutputDevice();
                Debug.Log("Tous les joueurs mute");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur toggle mute all: {e.Message}");
        }
    }

    /// <summary>
    /// Définir l'état mute de tous les autres joueurs
    /// </summary>
    public void SetMuteAll(bool muted)
    {
        if (!IsInChannel || _vivoxService.ActiveChannels == null) return;
        
        if (muted)
            _vivoxService.MuteOutputDevice();
        else
            _vivoxService.UnmuteOutputDevice();
    }

    /// <summary>
    /// Obtenir l'état mute des autres joueurs
    /// </summary>
    public bool IsAllMuted()
    {
        return _vivoxService?.IsOutputDeviceMuted ?? false;
    }

    /// <summary>
    /// Mute/Unmute un participant spécifique
    /// </summary>
    public async void SetParticipantMuted(string participantId, bool muted)
    {
        if (!IsInChannel || _vivoxService.ActiveChannels == null) return;
        
        try
        {
            if (muted)
            {
                await _vivoxService.BlockPlayerAsync(participantId);
                Debug.Log($"Participant {participantId} mute");
            }
            else
            {
                await _vivoxService.UnblockPlayerAsync(participantId);
                Debug.Log($"Participant {participantId} unmute");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur mute participant: {e.Message}");
        }
    }

    /// <summary>
    /// Définir le volume des haut-parleurs (-50 to 50)
    /// </summary>
    public void SetSpeakerVolume(int volume)
    {
        volume = Mathf.Clamp(volume, -50, 50);
        _vivoxService.SetOutputDeviceVolume(volume);
        Debug.Log($"Volume des haut-parleurs: {volume}");
    }

    /// <summary>
    /// Obtenir le volume des haut-parleurs (-50 to 50)
    /// </summary>
    public int GetSpeakerVolume()
    {
        return _vivoxService?.OutputDeviceVolume ?? defaultSpeakerVolume;
    }

    /// <summary>
    /// Définir le volume du microphone (-50 to 50)
    /// </summary>
    public void SetMicrophoneVolume(int volume)
    {
        volume = Mathf.Clamp(volume, -50, 50);
        _vivoxService.SetInputDeviceVolume(volume);
        Debug.Log($"Volume du microphone: {volume}");
    }

    /// <summary>
    /// Obtenir le volume du microphone (-50 to 50)
    /// </summary>
    public int GetMicrophoneVolume()
    {
        return _vivoxService?.InputDeviceVolume ?? defaultMicrophoneVolume;
    }

    /// <summary>
    /// Obtenir le nombre de participants dans le canal actuel
    /// </summary>
    public int GetParticipantCount()
    {
        if (!IsInChannel || _vivoxService.ActiveChannels == null) return 0;
        
        int count = 0;
        foreach (var channel in _vivoxService.ActiveChannels)
        {
            count += channel.Value.Count;
        }
        return count;
    }

    // Gestionnaires d'événements Vivox v16+
    private void OnVivoxLoggedIn()
    {
        OnLoginStatusChanged?.Invoke(true);
        Debug.Log("Vivox: Logged in");
    }

    private void OnVivoxLoggedOut()
    {
        OnLoginStatusChanged?.Invoke(false);
        Debug.Log("Vivox: Logged out");
    }

    private void OnVivoxChannelJoined(string channelName)
    {
        OnChannelConnectionChanged?.Invoke(true);
        Debug.Log($"Vivox: Joined channel {channelName}");
    }

    private void OnVivoxChannelLeft(string channelName)
    {
        OnChannelConnectionChanged?.Invoke(false);
        Debug.Log($"Vivox: Left channel {channelName}");
    }

    private void OnVivoxParticipantAdded(VivoxParticipant participant)
    {
        if (!participant.IsSelf)
        {
            Debug.Log($"Participant joined: {participant.DisplayName}");
            OnParticipantAdded?.Invoke(participant.PlayerId);
        }
    }

    private void OnVivoxParticipantRemoved(VivoxParticipant participant)
    {
        if (!participant.IsSelf)
        {
            Debug.Log($"Participant left: {participant.DisplayName}");
            OnParticipantRemoved?.Invoke(participant.PlayerId);
        }
    }

    private void OnDestroy()
    {
        if (_vivoxService != null)
        {
            _vivoxService.LoggedIn -= OnVivoxLoggedIn;
            _vivoxService.LoggedOut -= OnVivoxLoggedOut;
            _vivoxService.ChannelJoined -= OnVivoxChannelJoined;
            _vivoxService.ChannelLeft -= OnVivoxChannelLeft;
            _vivoxService.ParticipantAddedToChannel -= OnVivoxParticipantAdded;
            _vivoxService.ParticipantRemovedFromChannel -= OnVivoxParticipantRemoved;
        }
        
        LeaveChannel();
        Logout();
    }

    private void OnApplicationQuit()
    {
        LeaveChannel();
        Logout();
    }
}
