using UnityEngine;
using Unity.Services.Lobbies.Models;

public class LobbyInfoUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI lobbyNameText;
    [SerializeField] private TMPro.TextMeshProUGUI mapText;
    [SerializeField] private TMPro.TextMeshProUGUI playerCountText;
    [SerializeField] private TMPro.TextMeshProUGUI playerMaxCountText;
    private LobbyManager lobbyManager;
    private Lobby lobby;

    public void SetLobbyInfo(Lobby lobby, LobbyManager manager)
    {
        this.lobby = lobby;
        lobbyManager = manager;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count.ToString();
        playerMaxCountText.text = lobby.MaxPlayers.ToString();
        mapText.text = lobby.Data.ContainsKey("Map") ? lobby.Data["Map"].Value : "Unknown";
    }

    public void OnJoinButtonClicked()
    {
        if (lobbyManager != null && lobby != null)
        {
            Debug.Log("Joining lobby: " + lobby.Id);
            lobbyManager.JoinLobbyById(lobby.Id);
        }
    }
}
