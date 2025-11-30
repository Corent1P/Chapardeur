using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;

public class UIPlayerProfileCard : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private RawImage playerAvatarImage;
    [SerializeField] private Texture[] availableAvatars;
    [SerializeField] private Toggle readyToggle;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button banButton;

    [Header("Colors")]
    [SerializeField] private Color notReadyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);

    private string playerId;
    private LobbyManager lobbyManager;
    private bool isLocalPlayer;

    public void Setup(string id, string name, int avatarIndex, bool isReady, bool isMyCard, LobbyManager manager)
    {
        playerId = id;
        lobbyManager = manager;
        isLocalPlayer = isMyCard;

        playerNameText.text = name;
        
        if(avatarIndex >= 0 && avatarIndex < availableAvatars.Length)
            playerAvatarImage.texture = availableAvatars[avatarIndex];
        
        readyToggle.SetIsOnWithoutNotify(isReady);
        readyToggle.interactable = isLocalPlayer;
        
        UpdateStatusVisuals(isReady);

        readyToggle.onValueChanged.RemoveAllListeners();
        if (isLocalPlayer)
        {
            readyToggle.onValueChanged.AddListener(OnReadyToggled);
        }

        if (lobbyManager != null)
        {
            bool isHost = lobbyManager.joinLobby.HostId == AuthenticationService.Instance.PlayerId;
            kickButton.gameObject.SetActive(isHost && !isLocalPlayer);
            banButton.gameObject.SetActive(isHost && !isLocalPlayer);

            kickButton.onClick.RemoveAllListeners();
            banButton.onClick.RemoveAllListeners();

            if (isHost && !isLocalPlayer)
            {
                kickButton.onClick.AddListener(() => lobbyManager.KickPlayer(playerId));
                banButton.onClick.AddListener(() => lobbyManager.BanPlayer(playerId));
            }
        }
    }

    private void OnReadyToggled(bool isReady)
    {
        UpdateStatusVisuals(isReady);
        lobbyManager.UpdatePlayerReadyState(isReady);
    }

    private void UpdateStatusVisuals(bool isReady)
    {
        statusText.text = isReady ? "READY" : "NOT READY";
        statusText.color = isReady ? Color.green : Color.yellow;
    }

    public void ResetCard()
    {
        gameObject.SetActive(false);
    }
}