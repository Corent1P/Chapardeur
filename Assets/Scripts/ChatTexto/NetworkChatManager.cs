using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

public class NetworkChatManager : NetworkBehaviour
{
    public static NetworkChatManager Instance { get; private set; }
    public static event Action<ChatMessage> OnMessageReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Método para enviar mensaje público
    public void SendPublicMessage(string message)
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("NetworkChatManager no está spawnedo en la red");
            return;
        }

        string playerName = PlayerDataManager.Instance.PlayerName;

        if (IsServer)
        {
            // Servidor envía a todos los clientes
            BroadcastMessageClientRpc(playerName, message);
        }
        else
        {
            // Cliente envía al servidor
            SendMessageServerRpc(playerName, message, false, "");
        }
    }

    // Método para mensaje privado
    public void SendPrivateMessage(string targetPlayerName, string message)
    {
        if (!IsSpawned) return;

        string playerName = PlayerDataManager.Instance.PlayerName;

        // Encontrar ID del jugador objetivo
        ulong? targetClientId = FindClientIdByName(targetPlayerName);

        if (targetClientId.HasValue)
        {
            SendDirectMessageServerRpc(playerName, message, targetPlayerName, targetClientId.Value);
        }
        else
        {
            // Jugador no encontrado
            var errorMessage = new ChatMessage
            {
                SenderDisplayName = "System",
                MessageText = $"No se encontró al jugador '{targetPlayerName}'.",
                IsDirectMessage = false
            };
            OnMessageReceived?.Invoke(errorMessage);
        }
    }

    // Encontrar ClientId por nombre de jugador
    private ulong? FindClientIdByName(string playerName)
    {
        // Esta es una versión simplificada - necesitas mapear nombres a ClientIds
        // Puedes usar NetworkManager para esto

        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // Aquí necesitas una forma de obtener el nombre del jugador desde el NetworkClient
                // Por ahora, devolvemos un valor por defecto
                return client.ClientId;
            }
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string senderName, string message, bool isPrivate, string recipientName, ServerRpcParams rpcParams = default)
    {
        if (isPrivate)
        {
            // Para mensajes privados - necesitas implementar lógica específica
            BroadcastMessageClientRpc(senderName, message);
        }
        else
        {
            // Enviar a todos
            BroadcastMessageClientRpc(senderName, message);
        }
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string senderName, string message)
    {
        // No mostrar mensaje propio (el que envias)
        if (senderName == PlayerDataManager.Instance.PlayerName) return;

        var chatMessage = new ChatMessage
        {
            SenderDisplayName = senderName,
            MessageText = message,
            IsDirectMessage = false
        };
        OnMessageReceived?.Invoke(chatMessage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendDirectMessageServerRpc(string senderName, string message, string recipientName, ulong targetClientId)
    {
        SendDirectMessageClientRpc(senderName, message, recipientName);
    }

    [ClientRpc]
    private void SendDirectMessageClientRpc(string senderName, string message, string recipientName)
    {
        // Solo mostrar al destinatario
        if (recipientName == PlayerDataManager.Instance.PlayerName)
        {
            var chatMessage = new ChatMessage
            {
                SenderDisplayName = senderName,
                RecipientDisplayName = recipientName,
                MessageText = message,
                IsDirectMessage = true
            };
            OnMessageReceived?.Invoke(chatMessage);
        }
    }

    // Mensajes del sistema
    public void SendSystemMessage(string message)
    {
        var chatMessage = new ChatMessage
        {
            SenderDisplayName = "System",
            MessageText = message,
            IsDirectMessage = false
        };
        OnMessageReceived?.Invoke(chatMessage);
    }

    // IMPORTANTE: Añade este método para spawnear en la red
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("NetworkChatManager spawned en la red");
    }
}