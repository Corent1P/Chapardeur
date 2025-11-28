using UnityEngine;
using Unity.Netcode;

public class DestructibleWall : NetworkBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private GameObject pfBrokenWall;
    [SerializeField] private Transform explosionVfx;
    [SerializeField] private float explosionForce = 300f;

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!IsServer) return;

        health -= damage;
        if (health <= 0)
        {
            BreakWallClientRpc(hitPoint);

            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void BreakWallClientRpc(Vector3 hitPoint)
    {
        Vector3 explosionPoint = new Vector3(hitPoint.x, hitPoint.y + 0.7f, hitPoint.z);

        if (explosionVfx != null)
        {
            Instantiate(explosionVfx, explosionPoint, Quaternion.identity);
        }

        if (pfBrokenWall != null)
        {
            GameObject brokenWall = Instantiate(pfBrokenWall, transform.position, transform.rotation);
            foreach (Transform child in brokenWall.transform)
            {
                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddExplosionForce(explosionForce, explosionPoint, 5f);
                }
            }

            Destroy(brokenWall, 10f);
        }
    }
}