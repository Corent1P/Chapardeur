using UnityEngine;
using Unity.Services.Lobbies.Models;

public class ChatIntegration : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private NetworkChatManager chatManager;

    void OnEnable()
    {
        if (lobbyManager != null)
        {
            // Subscribe to lobby events
            // You'll need to add these events to your LobbyManager
            // LobbyManager.OnPlayerJoined += OnPlayerJoined;
            // LobbyManager.OnPlayerLeft += OnPlayerLeft;
        }
    }

    void OnDisable()
    {
        if (lobbyManager != null)
        {
            // Unsubscribe from events
            // LobbyManager.OnPlayerJoined -= OnPlayerJoined;
            // LobbyManager.OnPlayerLeft -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(Player player)
    {
        string playerName = player.Data.ContainsKey("PlayerName") ?
            player.Data["PlayerName"].Value : "Unknown";

        if (chatManager != null)
        {
            chatManager.SendSystemMessage($"{playerName} se ha unido al lobby");
        }
    }

    private void OnPlayerLeft(Player player)
    {
        string playerName = player.Data.ContainsKey("PlayerName") ?
            player.Data["PlayerName"].Value : "Unknown";

        if (chatManager != null)
        {
            chatManager.SendSystemMessage($"{playerName} ha abandonado el lobby");
        }
    }

    // Helper method to get player ID by name (add to LobbyManager if needed)
    public string GetPlayerIdByName(string playerName)
    {
        if (lobbyManager.joinLobby == null) return null;

        foreach (var player in lobbyManager.joinLobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                if (player.Data["PlayerName"].Value == playerName)
                {
                    return player.Id;
                }
            }
        }
        return null;
    }
}