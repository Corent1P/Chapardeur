using UnityEngine;
using Unity.Netcode;

public class LocalGameManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport.ConnectionData.Address == "127.0.0.1")
        {
            SpawnLocalPlayers();
        }
    }

    private void SpawnLocalPlayers()
    {
        int totalPlayers = PlayerPrefs.GetInt("LocalPlayerCount", 1);
        
        Debug.Log($"Spawning Local Players. Total expected : {totalPlayers}");

        for (int i = 1; i < totalPlayers; i++)
        {
            GameObject newPlayer = Instantiate(playerPrefab);

            newPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.ServerClientId);
        }
    }
}