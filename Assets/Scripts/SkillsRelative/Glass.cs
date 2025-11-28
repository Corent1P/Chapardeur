using UnityEngine;

public class Glass : MonoBehaviour
{
    private Collider glassCollider;

    private void Start()
    {
        glassCollider = GetComponent<Collider>();
        if (glassCollider == null)
            Debug.LogWarning("Collider component not found on Glass object.");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SkyWalker skyWalkerSkill = collision.gameObject.GetComponentInChildren<SkyWalker>();
            if (skyWalkerSkill != null)
            {
                skyWalkerSkill.SetAgainstGlass(true);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SkyWalker skyWalkerSkill = collision.gameObject.GetComponentInChildren<SkyWalker>();
            if (skyWalkerSkill != null)
            {
                skyWalkerSkill.SetAgainstGlass(false);
            }
        }
    }
}
