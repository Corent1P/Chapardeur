using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using UnityEditor;

public class LocalGameLauncher : MonoBehaviour
{
    [Header("Settings")]
    public string gameSceneName = "LocalGameScene";
    [Range(1, 4)] public int localPlayerCount = 1;
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

    public void SetLocalPlayerCount(int count)
    {
        localPlayerCount = Mathf.Clamp(count, 1, 4);
        Debug.Log($"Nombre de joueurs locaux défini à : {localPlayerCount}");
    }

    private void LoadGameScene()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName, 
            LoadSceneMode.Single
        );
    }
}