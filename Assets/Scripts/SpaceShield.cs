using UnityEngine;

public class SpaceShield : MonoBehaviour
{
    [SerializeField] private Vector3 respawnPosition;
    [SerializeField] private bool debugMode = false;

    private void Start()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugMode) return;

        if (other.CompareTag("Player"))
        {
            other.transform.position = respawnPosition;
        }
    }
}
