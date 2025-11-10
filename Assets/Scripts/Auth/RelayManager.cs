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

public class RelayManager : MonoBehaviour
{
    [Header("References")]
    public LobbyManager lobbyManager;

    [Header("UI References")]
    public GameObject lobbyWaitingUI; // Panel d'attente du lobby
    public GameObject startGameButton; // Bouton Start (visible uniquement pour l'hôte)
    public TextMeshProUGUI lobbyInfoText;
    public TextMeshProUGUI mapNameText;

    [Header("Game Settings")]
    public string gameSceneName = "Game"; // Nom de votre scène de jeu

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private float lobbyUpdateTimer;
    private float lobbyUpdateFrequency = 3f;
    private bool isHost = false;
    private bool hasJoinedRelay = false;
    private Tuple<string, string>[] listMaps; // (MapID, MapName)
    private int currentMapIndex = 0;

    private void Start()
    {
        listMaps = new Tuple<string, string>[]
        {
            new Tuple<string, string>("Game", "Mansion"),
            new Tuple<string, string>("Game-Procedural", "Random")
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
        try
        {
            // Vérifier que le lobby existe toujours
            if (lobbyManager.joinLobby == null)
            {
                Debug.LogWarning("Join lobby is null, stopping polling");
                return;
            }

            lobbyManager.joinLobby = await LobbyService.Instance.GetLobbyAsync(lobbyManager.joinLobby.Id);
            UpdatePlayerListUI();

            // Si on n'est pas l'hôte, vérifier si le Relay Code est disponible
            if (!isHost && lobbyManager.joinLobby.Data != null && lobbyManager.joinLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
            {
                string relayJoinCode = lobbyManager.joinLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                if (!string.IsNullOrEmpty(relayJoinCode) && relayJoinCode != "0")
                {
                    // L'hôte a démarré la partie, rejoindre via Relay
                    await JoinRelay(relayJoinCode);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobby polling failed: {e.Message} | Reason: {e.Reason} | Code: {e.ErrorCode}");

            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("Lobby no longer exists, stopping polling");
                lobbyManager.joinLobby = null;
                HideLobbyWaitingUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error in polling: {e.Message}");
        }
    }

    public void ShowLobbyWaitingUI(bool isHostPlayer)
    {
        isHost = isHostPlayer;

        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(true);

        if (startGameButton != null)
            startGameButton.SetActive(isHost);

        UpdatePlayerListUI();
    }

    public void HideLobbyWaitingUI()
    {
        if (lobbyWaitingUI != null)
            lobbyWaitingUI.SetActive(false);

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

        if (lobbyManager.joinLobby != null)
        {
            Debug.Log($"Players in lobby: {lobbyManager.joinLobby.Players.Count}/{lobbyManager.joinLobby.MaxPlayers}");
        }
    }

    public void NextMap()
    {
        currentMapIndex = (currentMapIndex + 1) % listMaps.Length;
        mapNameText.text = listMaps[currentMapIndex].Item2;
        gameSceneName = listMaps[currentMapIndex].Item1;
    }

    public void PreviousMap()
    {
        currentMapIndex = (currentMapIndex - 1 + listMaps.Length) % listMaps.Length;
        mapNameText.text = listMaps[currentMapIndex].Item2;
        gameSceneName = listMaps[currentMapIndex].Item1;
    }

    public string GetCurrentMapName()
    {
        return listMaps[currentMapIndex].Item2;
    }

    #endregion

    #region Relay Integration

    public async Task<string> CreateRelay()
    {
        try
        {
            Debug.Log("Creating Relay allocation...");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); 

            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("Relay Join Code created: " + relayJoinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Relay creation failed: " + e);
            return null;
        }
    }

    public async Task JoinRelay(string relayJoinCode)
    {
        if (hasJoinedRelay) return;

        try
        {
            Debug.Log("Joining game via Relay with code: " + relayJoinCode);

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
        }
    }

    public async void StartGame(string sceneToLoad)
    {
        gameSceneName = sceneToLoad;
        await StartGameInternal();
    }

    public async void StartGame()
    {
        await StartGameInternal();
    }

    private async Task StartGameInternal()
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

            string relayJoinCode = await CreateRelay();

            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("Failed to create relay!");
                return;
            }

            Debug.Log("Relay Join Code: " + relayJoinCode);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyManager.joinLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

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

    #endregion
}