using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Vector3 originalPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float openCloseDuration = 1f;
    private bool isOpen = false;
    private bool isMoving = false;

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
