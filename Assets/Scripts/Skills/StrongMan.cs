using Unity.Netcode;
using UnityEngine;

public class StrongMan : ASkills
{
    public float punchRange = 1.5f;


    public override void MainAction()
    {
        if (!IsOwner) return;

        print("StrongMan Main Action");
        Punch();
    }

    private void Punch()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, punchRange))
        {
            DestructibleWall wall = hit.collider.GetComponent<DestructibleWall>();
            
            if (wall != null)
            {
                var netObj = hit.collider.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    RequestPunchServerRpc(netObj.NetworkObjectId, hit.point);
                }
                else
                {
                    wall.TakeDamage(25, hit.point);
                }
            }
        }
    }

    [ServerRpc]
    private void RequestPunchServerRpc(ulong targetObjId, Vector3 hitPoint)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjId, out NetworkObject netObj))
        {
            var wall = netObj.GetComponent<DestructibleWall>();
            if (wall != null)
            {
                wall.TakeDamage(25, hitPoint); 
            }
        }
    }

    public override void SecondaryAction()
    {
        if (!IsOwner) return;
        print("StrongMan Secondary Action");
    }
}