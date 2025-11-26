using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private GameObject pfBrokenWall;
    [SerializeField] private Transform explosionVfx;
    [SerializeField] private float explosionForce = 300f;

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        health -= damage;
        if (health <= 0)
        {
            Vector3 explosionPoint = new Vector3(hitPoint.x, hitPoint.y + 0.7f, hitPoint.z);
            Instantiate(explosionVfx, explosionPoint, Quaternion.identity);
            GameObject brokenWall = Instantiate(pfBrokenWall, transform.position, transform.rotation);
            foreach (Transform child in brokenWall.transform)
            {
                if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddExplosionForce(explosionForce, explosionPoint, 5f);
                }
            }
            Destroy(gameObject);
        }
    }
}
