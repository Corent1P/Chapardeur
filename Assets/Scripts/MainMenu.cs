using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private MenuManager menuManager;

    void Start()
    {
        #if !DISABLE_ONLINE
            playButton.onClick.AddListener(StartOnlineGame);
        #else
            playButton.onClick.AddListener(StartLocalGame);
        #endif

    }

    private void StartOnlineGame()
    {
        #if !DISABLE_ONLINE
            // Logique de connexion Relay / Lobby
            Debug.Log("Connexion en cours...");
            menuManager?.ShowAuthMenu();
        #endif
    }

    private void StartLocalGame()
    {
        #if !DISABLE_ONLINE
            // Logique Loopback (Safe pour Xbox)
            Debug.Log("Lancement Local...");
            menuManager?.ShowPlayLocalMenu();
        #endif
    }
}