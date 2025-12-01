using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ChatBehaviour : NetworkBehaviour
{
    [SerializeField] private GameObject chatUI = null;
    [SerializeField] private TMP_Text chatText = null;
    [SerializeField] private TMP_InputField inputField = null;

    private static event Action<string> OnMessage;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        chatUI.SetActive(true);
        OnMessage += HandleNewMessage;
    }

    private void OnDestroy()
    {
        if (!IsOwner) return;

        OnMessage -= HandleNewMessage;
    }

    private void HandleNewMessage(string message)
    {
        chatText.text += message;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (string.IsNullOrWhiteSpace(inputField.text)) return;

            SendMessageServerRpc(inputField.text);
            inputField.text = string.Empty;
        }
    }

    [ServerRpc]
    private void SendMessageServerRpc(string message)
    {
        ReceiveMessageClientRpc($"[{OwnerClientId}]: {message}");
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(string message)
    {
        OnMessage?.Invoke($"\n{message}");
    }
}