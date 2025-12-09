using UnityEngine;

public class ReactorColumnRiser : MonoBehaviour
{
    [SerializeField] private GameObject reactorColumn;
    [SerializeField] private float targetHeight = 0f;
    [SerializeField] private float riseSpeed = 1f;
    private float initialHeight;

    private void Start()
    {
        if (reactorColumn != null)
        {
            initialHeight = reactorColumn.transform.position.y;
        }
    }

    public void RiseColumn()
    {
        StartCoroutine(RiseCoroutine());
    }

    private System.Collections.IEnumerator RiseCoroutine()
    {
        Vector3 startPosition = reactorColumn.transform.position;
        Vector3 endPosition = new Vector3(startPosition.x, targetHeight, startPosition.z);
        float elapsedTime = 0f;
        float duration = Mathf.Abs(targetHeight - startPosition.y) / riseSpeed;

        while (elapsedTime < duration)
        {
            reactorColumn.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        reactorColumn.transform.position = endPosition;
    }

    public void ResetColumn()
    {
        StartCoroutine(ResetCoroutine());
    }

    private System.Collections.IEnumerator ResetCoroutine()
    {
        Vector3 startPosition = reactorColumn.transform.position;
        Vector3 endPosition = new Vector3(startPosition.x, initialHeight, startPosition.z);
        float elapsedTime = 0f;
        float duration = Mathf.Abs(initialHeight - startPosition.y) / (riseSpeed * 2);

        while (elapsedTime < duration)
        {
            reactorColumn.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        reactorColumn.transform.position = endPosition;
    }
}
