using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using TMPro;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public Lobby hostLobby;
    public Lobby joinLobby;
    private float heartbeatTimer;
    private float heartbeatFrequency = 15f;
    public MenuManager menuManager;
    public TMP_InputField inputFieldCode;
    public TMP_InputField inputFieldName;
    public TextMeshProUGUI lobbyCodeText;
    public TextMeshProUGUI joinErrorText;
    public TextMeshProUGUI joinSuccessText;
    public RelayManager relayManager;
    public TextMeshProUGUI maxPlayersText;
    public int maxPlayers = 4;
    private int minMaxPlayers = 2;
    private int maxMaxPlayers = 6;

    private async void Start()
    {
        #if (!DISABLE_ONLINE)
            // Sur Xbox, on désactive ce composant immédiatement
            // car on n'a pas le droit d'utiliser l'Auth Unity.
            this.enabled = false; 
            return;
        #endif
        Debug.Log("Starting LobbyManager...");
        // S'assurer que les services sont initialisés
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();
        }
        maxPlayersText.text = (char)('0' + maxPlayers) + "";
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    public void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = heartbeatFrequency;
                SendHeartbeat();
            }
        }
    }

    private async void SendHeartbeat()
    {
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            Debug.Log("Heartbeat sent to lobby: " + hostLobby.Name);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers = 4, string map = "Arena", bool IsPrivate = false, string gameMode = "Deathmatch")
    {
        try
        {
            Player player = await GetPlayer();

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = IsPrivate,
                Player = player,

                Data = new Dictionary<string, DataObject>
                {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) },
                    { "Map", new DataObject(DataObject.VisibilityOptions.Public, map) },
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            };

            hostLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            joinLobby = hostLobby;

            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = "Lobby Code: " + joinLobby.LobbyCode;
            }

            Debug.Log("Party created: " + hostLobby.Name + " | Players: " + hostLobby.Players.Count + "/" + hostLobby.MaxPlayers + " | Lobby Code: " + hostLobby.LobbyCode);

            // Afficher l'UI d'attente pour l'hôte
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(true);
            if (menuManager != null)
                menuManager.HideAllMenus();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public void CreateLobby()
    {
        if (string.IsNullOrEmpty(inputFieldName.text))
            CreateLobby("Default Lobby", maxPlayers, relayManager.GetCurrentMapName());
        else
            CreateLobby(inputFieldName.text, maxPlayers, relayManager.GetCurrentMapName());
    }

    public void increaseMaxPlayers()
    {
        if (maxPlayers < maxMaxPlayers)
        {
            maxPlayers++;
            maxPlayersText.text = (char)('0' + maxPlayers) + "";
        }
    }

    public void decreaseMaxPlayers()
    {
        if (maxPlayers > minMaxPlayers)
        {
            maxPlayers--;
            maxPlayersText.text = (char)('0' + maxPlayers) + "";
        }
    }

    public void SetMaxPlayer(int value)
    {
        maxPlayers = value;
        Debug.Log("Max players set to: " + maxPlayers);
    }

    private List<Lobby> cachedLobbies = new List<Lobby>();

    public async void ListLobbies()
    {
        cachedLobbies.Clear();
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + response.Results.Count);

            foreach (Lobby lobby in response.Results)
            {
                Debug.Log("Lobby Name: " + lobby.Name + " | Players: " + lobby.Players.Count + "/" + lobby.MaxPlayers + " | Lobby Code: " + lobby.LobbyCode);
            }

            cachedLobbies = response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }

    public List<Lobby> GetCachedLobbies()
    {
        return cachedLobbies;
    }

    public async void QuickJoinLobby()
    {
        try
        {
            Player player = await GetPlayer();

            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
            {
                Player = player
            };

            joinLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log("Quick joined lobby: " + joinLobby.Name);
            PrintPlayers(joinLobby);
            joinErrorText.gameObject.SetActive(false);
            joinSuccessText.gameObject.SetActive(true);

            // 🔥 AJOUT CRITIQUE : Afficher l'UI d'attente pour le client
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(false); // false = n'est pas l'hôte
            if (menuManager != null)
                menuManager.HideAllMenus();
        }
        catch (LobbyServiceException e)
        {
            joinSuccessText.gameObject.SetActive(false);
            joinErrorText.gameObject.SetActive(true);
            Debug.LogException(e);
        }
    }

    public void JoinLobby(Lobby lobby)
    {
        JoinLobbyById(lobby.Id);
    }

    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            Player player = await GetPlayer();
            
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = player
            };
            
            joinLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            Debug.Log("Joined lobby: " + joinLobby.Name);
            PrintPlayers(joinLobby);
            joinErrorText.gameObject.SetActive(false);
            joinSuccessText.gameObject.SetActive(true);
            
            // 🔥 AJOUT CRITIQUE : Afficher l'UI d'attente pour le client
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(false); // false = n'est pas l'hôte
            if (menuManager != null)
                menuManager.HideAllMenus();
        }
        catch (LobbyServiceException e)
        {
            joinSuccessText.gameObject.SetActive(false);
            joinErrorText.gameObject.SetActive(true);
            Debug.LogException(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Player player = await GetPlayer();

            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = player
            };

            joinLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log("Joined lobby with code: " + lobbyCode);
            PrintPlayers(joinLobby);
            joinErrorText.gameObject.SetActive(false);
            joinSuccessText.gameObject.SetActive(true);

            // 🔥 AJOUT CRITIQUE : Afficher l'UI d'attente pour le client
            if (relayManager != null)
                relayManager.ShowLobbyWaitingUI(false); // false = n'est pas l'hôte
            if (menuManager != null)
                menuManager.HideAllMenus();
        }
        catch (LobbyServiceException e)
        {
            joinSuccessText.gameObject.SetActive(false);
            joinErrorText.gameObject.SetActive(true);
            Debug.LogException(e);
        }
    }

    public void JoinLobbyByCode()
    {
        if (string.IsNullOrEmpty(inputFieldCode.text))
            Debug.Log("Please enter a valid lobby code.");
        else
            JoinLobbyByCode(inputFieldCode.text);
    }

    public void PrintPlayers()
    {
        PrintPlayers(hostLobby);
    }

    public void PrintPlayers(Lobby lobby)
    {
        if (lobby == null) return;
        
        foreach (Player player in lobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
            {
                Debug.Log("Player ID: " + player.Id + " | Player Name: " + player.Data["PlayerName"].Value);
            }
        }
    }

    public async Task<Player> GetPlayer()
    {
        if (PlayerDataManager.Instance.PlayerName == "New Player")
            await PlayerDataManager.Instance.LoadProfile();

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerDataManager.Instance.PlayerName) },
                { "AvatarId", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerDataManager.Instance.AvatarId.ToString()) },
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") } // Par défaut pas prêt
            }
        };
    }

    public async void UpdatePlayerReadyState(bool isReady)
    {
        if (joinLobby == null) return;

        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady ? "true" : "false") }
                }
            };

            await LobbyService.Instance.UpdatePlayerAsync(joinLobby.Id, playerId, options);
            Debug.Log("Statut Ready mis à jour : " + isReady);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Erreur mise à jour ready : " + e);
        }
    }

    public bool CheckAllPlayersReady()
    {
        if (joinLobby == null) return false;
        
        // On ne démarre pas une game tout seul (sauf pour debug)
        // TODO
        // if (joinLobby.Players.Count < 2) return false; 

        foreach (var player in joinLobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("IsReady"))
            {
                if (player.Data["IsReady"].Value != "true")
                    return false; // Un joueur n'est pas prêt
            }
            else
            {
                return false; // Donnée manquante = pas prêt
            }
        }
        return true;
    }

    // Méthode pour quitter proprement un lobby
    public async void LeaveLobby()
    {
        try
        {
            if (joinLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(joinLobby.Id, AuthenticationService.Instance.PlayerId);
                joinLobby = null;
                hostLobby = null;
                
                if (relayManager != null)
                {
                    relayManager.HideLobbyWaitingUI();
                }
                
                Debug.Log("Left lobby successfully");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }
}