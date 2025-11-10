using UnityEngine;
using System.Threading.Tasks;

public class LobbyChatManager : MonoBehaviour
{
    [SerializeField] private ChatUI chatUI;
    [SerializeField] private VivoxManager vivoxManager;
    [SerializeField] private TestLobby testLobby;

    private const string LOBBY_CHANNEL = "Lobby";

    private async void Start()
    {
        await WaitForAuthentication();

        await InitializeVivox();
    }

    private async Task WaitForAuthentication()
    {
        while (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
        {
            await Task.Delay(100);
        }
    }

    private async Task InitializeVivox()
    {
        if (!vivoxManager) return;

        try
        {
            await vivoxManager.LoginVivox();

            await vivoxManager.JoinTextChannel(LOBBY_CHANNEL);

            Debug.Log("Chat del lobby inicializado correctamente");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error inicializando chat del lobby: {ex.Message}");
        }
    }

    public async void OnLeaveLobby()
    {
        if (vivoxManager)
        {
            await vivoxManager.LeaveTextChannel(LOBBY_CHANNEL);
        }
    }

    public async void OnGameStart()
    {
        if (vivoxManager)
        {
            await vivoxManager.LeaveTextChannel(LOBBY_CHANNEL);

            await vivoxManager.JoinTextChannel("Game");
        }
    }

    private void OnDestroy()
    {
        OnLeaveLobby();
    }
}