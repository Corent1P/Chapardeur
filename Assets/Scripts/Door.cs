using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Vector3 originalPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float openCloseDuration = 1f;
    [SerializeField] private AudioClip doorSound;
    private bool isOpen = false;
    private bool isMoving = false;

    private NetworkSoundManager networkSoundManager;
    private SoundManager localSoundManager;

    void Start()
    {
        networkSoundManager = FindAnyObjectByType<NetworkSoundManager>();
        localSoundManager = FindAnyObjectByType<SoundManager>();
    }

    public void ToggleDoor()
    {
        if (isMoving) return;

        if (isOpen)
        {
            StartCoroutine(MoveDoor(targetPosition, originalPosition));
        }
        else
        {
            StartCoroutine(MoveDoor(originalPosition, targetPosition));
        }
        isOpen = !isOpen;
    }

    private System.Collections.IEnumerator MoveDoor(Vector3 from, Vector3 to)
    {
        isMoving = true;
        float elapsed = 0f;

        if (networkSoundManager != null)
        {
            networkSoundManager.PlaySoundAtPosition(doorSound, transform.position);
        }
        else if (localSoundManager != null)
        {
            localSoundManager.PlaySound(doorSound, transform.position);
        }

        while (elapsed < openCloseDuration)
        {
            transform.localPosition = Vector3.Lerp(from, to, elapsed / openCloseDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = to;
        isMoving = false;
    }
}
