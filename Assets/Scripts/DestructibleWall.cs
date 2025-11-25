using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private GameObject pfBrokenWall;
    [SerializeField] private Transform explosionVfx;
    private Vector3 meshSizeRatio;

    private void Start()
    {
        meshSizeRatio = new Vector3(14f, 6f, 0.2f);
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        health -= damage;
        if (health <= 0)
        {
            Vector3 explosionPoint = new Vector3(hitPoint.x, hitPoint.y + 0.7f, hitPoint.z);
            Instantiate(explosionVfx, explosionPoint, Quaternion.identity);
            GameObject brokenWall = SpawnDestroyedWall();
            foreach (Transform child in brokenWall.transform)
            {
                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddExplosionForce(1000f, explosionPoint, 10f);
                }
            }
            Destroy(gameObject);
        }
    }

    private GameObject SpawnDestroyedWall()
    {
        GameObject newWall = Instantiate(pfBrokenWall, transform.position, transform.rotation);
        Vector3 newScale = new Vector3(
            transform.localScale.x / meshSizeRatio.x,
            transform.localScale.y / meshSizeRatio.y,
            transform.localScale.z / meshSizeRatio.z
        );

        newWall.transform.localScale = newScale;
        return newWall;
    }
}
