using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class LobbyButtons : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    void Start()
    {
        // Asegurar cursor visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Configurar botones
        hostButton.onClick.AddListener(HostLobby);
        joinButton.onClick.AddListener(JoinLobby);
    }

    public void HostLobby()
    {
        // El cursor debería mantenerse visible después de hostear
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host started successfully");
        }
    }

    public void JoinLobby()
    {
        // El cursor debería mantenerse visible después de unirse
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client started successfully");
        }
    }
}