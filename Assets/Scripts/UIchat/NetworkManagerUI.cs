using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button serverButton;

    void Start()
    {
        // Asegurar cursor visible al inicio
        SetCursorVisible(true);

        hostButton.onClick.AddListener(() =>
        {
            SetCursorVisible(true);
            NetworkManager.Singleton.StartHost();
        });

        clientButton.onClick.AddListener(() =>
        {
            SetCursorVisible(true);
            NetworkManager.Singleton.StartClient();
        });

        serverButton.onClick.AddListener(() =>
        {
            SetCursorVisible(true);
            NetworkManager.Singleton.StartServer();
        });
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void Update()
    {
        // Verificación constante
        if (!Cursor.visible)
        {
            SetCursorVisible(true);
        }
    }
}