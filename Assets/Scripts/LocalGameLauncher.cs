using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class LocalGameLauncher : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "Game";
    
    [Range(1, 4)]
    public int localPlayerCount = 2;

    public void StartLocalSession()
    {
        Debug.Log($"Préparation session locale pour {localPlayerCount} joueurs...");

        PlayerPrefs.SetInt("LocalPlayerCount", localPlayerCount);
        PlayerPrefs.Save();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7777);
        // transport.UseRelay = false;

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host Local Démarré !");
            LoadGameScene();
        }
        else
        {
            Debug.LogError("Impossible de démarrer le Host Local.");
        }
    }

    private void LoadGameScene()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName, 
            LoadSceneMode.Single
        );
    }
}