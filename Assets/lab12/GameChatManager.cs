using UnityEngine;
using System.Threading.Tasks;

public class GameChatManager : MonoBehaviour
{
    [SerializeField] private ChatUI chatUI;
    [SerializeField] private VivoxManager vivoxManager;

    private const string GAME_CHANNEL = "Game";

    private async void Start()
    {
        await InitializeGameChat();
    }

    private async Task InitializeGameChat()
    {
        if (!vivoxManager) return;

        try
        {
            if (!Unity.Services.Vivox.VivoxService.Instance.IsLoggedIn)
            {
                await vivoxManager.LoginVivox();
            }

            await vivoxManager.JoinTextChannel(GAME_CHANNEL);

            Debug.Log("Chat del juego inicializado correctamente");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error inicializando chat del juego: {ex.Message}");
        }
    }

    public async void OnGameEnd()
    {
        if (vivoxManager)
        {
            await vivoxManager.LeaveTextChannel(GAME_CHANNEL);
        }
    }

    private void OnDestroy()
    {
        OnGameEnd();
    }
}