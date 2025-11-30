using UnityEngine;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [Header("References")]
    public LobbyManager lobbyManager;
    public Transform cardsContainer; // Le parent (Grid Layout Group)
    public UIPlayerProfileCard[] playerCards; // Tableau fixe de tes 4 cartes UI dans la scène
    public Button startGameButton;

    private void Update()
    {
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        Lobby currentLobby = lobbyManager.joinLobby;

        if (currentLobby == null)
        {
            foreach (var card in playerCards) card.gameObject.SetActive(false);
            if(startGameButton) startGameButton.gameObject.SetActive(false);
            return;
        }

        List<Player> players = currentLobby.Players;

        for (int i = 0; i < playerCards.Length; i++)
        {
            if (i < players.Count)
            {
                Player p = players[i];
                playerCards[i].gameObject.SetActive(true);

                string pName = p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Unknown";
                int avatarId = 0;
                if (p.Data.ContainsKey("AvatarId")) int.TryParse(p.Data["AvatarId"].Value, out avatarId);
                bool isReady = p.Data.ContainsKey("IsReady") && p.Data["IsReady"].Value == "true";

                bool isMe = p.Id == AuthenticationService.Instance.PlayerId;

                playerCards[i].Setup(p.Id, pName, avatarId, isReady, isMe, lobbyManager);
            }
            else
            {
                playerCards[i].gameObject.SetActive(false);
            }
        }

        if (startGameButton != null)
        {
            bool isHost = currentLobby.HostId == AuthenticationService.Instance.PlayerId;
            startGameButton.gameObject.SetActive(isHost);
            
            if (isHost)
            {
                bool allReady = lobbyManager.CheckAllPlayersReady();
                startGameButton.gameObject.SetActive(allReady);
            }
        }
    }
}