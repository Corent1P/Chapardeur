using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Lobbies;
using Sirenix.OdinInspector;

using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System;

public class TestLobby : MonoBehaviour
{
    public RelayManager relayManager;
    public NetworkSceneManager networkSceneManager;


    public Lobby HostLobby;
    public Lobby JoinedLobby;

    private float heartBeatTimer;
    private float lobbyUpdateTimer;

    //->Retorna todas los lobbys
    public Action<List<Lobby>> OnLobbyRefresh;


    public const string KEY_RELAYCODE = "keyRelayCode";

    [SerializeField] private LobbyChatManager lobbyChatManager;

    private async void Start()
    {
        //await UnityServices.InitializeAsync();
    }
    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdates();
    }
    public async void HandleLobbyHeartBeat()
    {
        if (HostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0)
            {
                float heartbeatTimerMax = 10;
                heartBeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(HostLobby.Id);
                Debug.Log("HeartBeat");
            }
        }
    }
    public async void HandleLobbyPollForUpdates()
    {
        if (JoinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float LobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = LobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobby.Id);
                JoinedLobby = lobby;
            }

            if (JoinedLobby.Data[KEY_RELAYCODE].Value != "0")
            {
                if(!IsLobbyHost())
                {
                    relayManager.JoinRelay(JoinedLobby.Data[KEY_RELAYCODE].Value);
                }
                JoinedLobby = null; 
            }
        }
    }
    
    //-> UI LLAMAR DESDE LA UI
    [Button]
    public async void CreateLobby(string lobbyName, int maxPlayers =4, bool isPrivate = false, string gameMode = "CTF", string map = "Peru")
    {
        try
        {
            //Player player = await GetPlayer();
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,

                Player = await GetPlayer(),

                Data = new Dictionary<string, DataObject>
                {

                    {KEY_RELAYCODE ,new DataObject(DataObject.VisibilityOptions.Member,"0") },//->
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public,gameMode) },
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public,map) },
                }

            };

            Lobby lobby =  await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers , options);

            HostLobby = lobby;
            JoinedLobby = HostLobby;
            Debug.Log("Partida creada" + lobby.Name + "Max player : " + lobby.MaxPlayers + "Joincode: " + lobby.LobbyCode);
        }
        catch(LobbyServiceException ex)
        {
            Debug.Log(ex);
        } 
    }

    //-> UI REFRESH BUTTON
    [Button]
    public async void ListLobbies()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Se encontraron los lobbies: " + queryResponse.Results.Count);

            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log("Lobby Name: " + lobby.Name + "Max player : " + lobby.MaxPlayers + "Joincode: " + lobby.LobbyCode);
              //  lobby.Players[0].Data["PlayerName"]

            }
            OnLobbyRefresh?.Invoke(queryResponse.Results);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    [Button]
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            //Player player = await GetPlayer();
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions()
            {
                Player = await GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            JoinedLobby = lobby;
            Debug.Log("Te uniste al lobby!!!   " + lobby.Name);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    //->llamar desde la UI
    [Button]
    public async void JoinLobbyByID(string lobbyCode)
    {
        try
        {
            //Player player = await GetPlayer();
            JoinLobbyByIdOptions joinLobbyByCodeOptions = new JoinLobbyByIdOptions()
            {
                Player = await GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyCode, joinLobbyByCodeOptions);

            JoinedLobby = lobby;
            Debug.Log("Te uniste al lobby!!!   " + lobby.Name);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public void PrintPlayer()
    {
        PrintPlayers(JoinedLobby);
    }

    public void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Lobby Name: " + lobby.Name + "Max player : " + lobby.MaxPlayers + "Joincode: ");
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }

      //  lobby.Id
         // lobby.LobbyCode   
    }
    [Button]
    public async Task<Player> GetPlayer()
    {
        string nickName = await AuthenticationService.Instance.GetPlayerNameAsync();

        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
        {
            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nickName) }
        }
        };
    }
    public async void QuickJoin()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();

        }
        catch(LobbyServiceException ex)
        {
            Debug.LogException (ex);
        }

    }
    [Button]
    public async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync
                (
                 HostLobby.Id,
                 new UpdateLobbyOptions
                 {
                     Data = new Dictionary<string, DataObject>
                     {
                         {"GameMode", new DataObject(DataObject.VisibilityOptions.Member, gameMode) }
                     }
                 }


                );
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    
    [Button]
    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    [Button]
    public async void KickPlayer(string playerID)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(JoinedLobby.Id, playerID);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    [Button]
    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(JoinedLobby.Id);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }
    [Button]
    public async void MigrateLobbyHost(string playerID)
    {
        try
        {
            HostLobby = await LobbyService.Instance.UpdateLobbyAsync(HostLobby.Id,
                new UpdateLobbyOptions
                {
                    HostId = playerID,
                }
                );
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }


    [Button]
    public async void StartGame()
    {
        if (!IsLobbyHost()) return;
        try
        {
            string relayCode = await relayManager.CreateRelay();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(JoinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
             {
                 {KEY_RELAYCODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
             }

            });
            JoinedLobby = lobby;

            networkSceneManager.LoadGameScene("GameScene");
        }
        catch (LobbyServiceException ex)
        {

            Debug.LogError(ex); 
        }
    }

    public bool IsLobbyHost()
    {
        return HostLobby != null;
    }

    [Button]
    public async void StartGame()
    {
        if (!IsLobbyHost()) return;
        try
        {
            if (lobbyChatManager != null)
            {
                lobbyChatManager.OnGameStart();
            }

            string relayCode = await relayManager.CreateRelay();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(JoinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_RELAYCODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });
            JoinedLobby = lobby;

            networkSceneManager.LoadGameScene("GameScene");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }
    }

    public void OnLeaveLobby()
    {
        if (lobbyChatManager != null)
        {
            lobbyChatManager.OnLeaveLobby();
        }
    }
    [Button]
    public async void StartGame()
    {
        if (!IsLobbyHost()) return;
        try
        {
            if (lobbyChatManager != null)
            {
                lobbyChatManager.OnGameStart();
            }

            string relayCode = await relayManager.CreateRelay();

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(JoinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_RELAYCODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });
            JoinedLobby = lobby;

            networkSceneManager.LoadGameScene("GameScene");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex);
        }
    }

    public void OnLeaveLobby()
    {
        if (lobbyChatManager != null)
        {
            lobbyChatManager.OnLeaveLobby();
        }
    }
}
