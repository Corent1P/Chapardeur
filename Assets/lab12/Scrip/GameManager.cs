using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject playerPrefab;


    public override void OnNetworkSpawn()
    {
        ulong id =  NetworkManager.Singleton.LocalClientId;
        SpawnPlayerRpc(id);

    }

    [Rpc(SendTo.Server)]
    public void SpawnPlayerRpc(ulong id)
    {
        Vector3 spawnPos = new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));

        GameObject player = Instantiate(playerPrefab,spawnPos , Quaternion.identity);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(id);

    }
}
