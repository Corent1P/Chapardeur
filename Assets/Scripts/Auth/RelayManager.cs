using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class RelayManager : MonoBehaviour
{
    [Header("References")]
    public LobbyManager lobbyManager;

    [Header("UI References")]
    public GameObject lobbyWaitingUI;
    public GameObject lobbyWaitingFirstButton;
    public GameObject voiceChatLobby;
    // public Transform playerListContainer; // Container pour la liste des joueurs
    // public GameObject playerListItemPrefab; // Prefab pour afficher un joueur
    public GameObject startGameButton; // Bouton Start (visible uniquement pour l'hôte)
    public TextMeshProUGUI lobbyInfoText;
    public TextMeshProUGUI mapNameText;

    [Header("Game Settings")]
    public string gameSceneName = "MuseumGameScene";

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private float lobbyUpdateTimer;
    private float lobbyUpdateFrequency = 3f;
    private bool isHost = false;
    private bool hasJoinedRelay = false;
    private Tuple<string, string>[] listMaps; // (MapID, MapName)
    private int currentMapIndex = 0;
    private bool isConnectingToRelay = false;

    private void Start()
    {
        #if DISABLE_ONLINE
            // Sur Xbox, on désactive ce composant immédiatement
            // car on n'a pas le droit d'utiliser l'Auth Unity.
            this.enabled = false; 
            return;
        #endif
        listMaps = new Tuple<string, string>[]
        {
            new Tuple<string, string>("MuseumGameScene", "Museum"),
            new Tuple<string, string>("BankGameScene", "Bank")
        };

        mapNameText.text = listMaps[0].Item2;
        gameSceneName = listMaps[0].Item1;
    }

    private void Update()
    {
        HandleLobbyPolling();
    }

    #region Lobby Polling & UI

    private void HandleLobbyPolling()
    {
        if (lobbyManager.joinLobby != null && !hasJoinedRelay)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer <= 0f)
            {
                lobbyUpdateTimer = lobbyUpdateFrequency;
                PollLobby();
            }
        }
    }

    private async void PollLobby()
    {

        if (hasJoinedRelay || isConnectingToRelay) return;

        try
        {
            if (lobbyManager.joinLobby == null) return;

            lobbyManager.joinLobby = await LobbyService.Instance.GetLobbyAsync(lobbyManager.joinLobby.Id);
            UpdatePlayerListUI();
            
            if (!isHost && lobbyManager.joinLobby.Data != null && lobbyManager.joinLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
            {
                string relayJoinCode = lobbyManager.joinLobby.Data[KEY_RELAY_JOIN_CODE].Value;

                if (!string.IsNullOrEmpty(relayJoinCode) && relayJoinCode != "0")
                {
                    isConnectingToRelay = true; 
                    await JoinRelay(relayJoinCode);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                lobbyManager.joinLobby = null;
                HideLobbyWaitingUI();
            }
        }
    }

    public void ShowLobbyWaitingUI(bool isHostPlayer)
    {
        isHost = isHostPlayer;

        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(true);

        EventSystem.current.SetSelectedGameObject(lobbyWaitingFirstButton);

        if (voiceChatLobby != null)
            voiceChatLobby.SetActive(true);

        if (startGameButton != null)
            startGameButton.SetActive(isHost);

        UpdatePlayerListUI();
    }

    public void HideLobbyWaitingUI()
    {
        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);

        if (voiceChatLobby != null)
            voiceChatLobby.SetActive(false);
        
        isHost = false;
        hasJoinedRelay = false;
    }

    private void UpdatePlayerListUI()
    {
        if (lobbyInfoText != null)
        {
            int currentPlayers = lobbyManager.joinLobby != null ? lobbyManager.joinLobby.Players.Count : 0;
            int maxPlayers = lobbyManager.joinLobby != null ? lobbyManager.joinLobby.MaxPlayers : 0;
            string lobbyName = lobbyManager.joinLobby != null ? lobbyManager.joinLobby.Name : "N/A";
            lobbyInfoText.text = $"Lobby Name: {lobbyName}\nPlayers: {currentPlayers}/{maxPlayers}";
        }
        Debug.Log($"Players in lobby: {lobbyManager.joinLobby.Players.Count}/{lobbyManager.joinLobby.MaxPlayers}");
    }

    public void NextMap()
    {
        currentMapIndex = (currentMapIndex + 1) % listMaps.Length;
        mapNameText.text = listMaps[currentMapIndex].Item2;
        gameSceneName = listMaps[currentMapIndex].Item1;
        Debug.Log("Selected Map: " + gameSceneName);
    }

    public void PreviousMap()
    {
        currentMapIndex = (currentMapIndex - 1 + listMaps.Length) % listMaps.Length;
        mapNameText.text = listMaps[currentMapIndex].Item2;
        gameSceneName = listMaps[currentMapIndex].Item1;
        Debug.Log("Selected Map: " + gameSceneName);
    }

    public string GetCurrentMapName()
    {
        return listMaps[currentMapIndex].Item2;
    }

    #endregion

    #region Relay Integration

    public void StartGame(string sceneToLoad)
    {
        gameSceneName = sceneToLoad;
        StartGame();
    }

    public async void StartGame()
    {
        if (!isHost)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }

        if (lobbyManager.joinLobby == null)
        {
            Debug.LogWarning("No active lobby!");
            return;
        }

        try
        {
            Debug.Log("Starting game and creating Relay allocation...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(lobbyManager.joinLobby.MaxPlayers - 1);
            
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            Debug.Log("Relay Join Code: " + relayJoinCode);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyManager.joinLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            hasJoinedRelay = true;
            PlayerPrefs.SetInt("MaxPlayers", lobbyManager.joinLobby.MaxPlayers);

            var status = NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogError($"Failed to load scene {gameSceneName}. Status: {status}");
            }

            Debug.Log("Game started successfully!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay creation failed: " + e);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Lobby update failed: " + e);
        }
    }

    private async Task JoinRelay(string relayJoinCode)
    {
        if (hasJoinedRelay) return;

        try
        {
            Debug.Log("Attempting to join Relay...");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            hasJoinedRelay = true;
            Debug.Log("Successfully joined Relay!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay join failed: " + e);
            isConnectingToRelay = false; 
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unexpected error joining Relay: " + e);
            isConnectingToRelay = false;
        }
    }

    #endregion
}