using UnityEngine;

public class Bridge : MonoBehaviour
{
    [SerializeField] GameObject[] bridgeSections;
    [SerializeField] float bridgeMoveSpeed = 2f;
    [SerializeField] float distanceBridgeSectionToMove = 1f;
    [SerializeField] Vector3 bridgeDirection = Vector3.forward;

    private bool isBridgeOpen = false;
    private bool isMoving = false;

    public void OpenBridge()
    {
        if (!isBridgeOpen && !isMoving)
        {
            StartCoroutine(MoveBridgeSections());
        }
    }

    // Move a bridge section at bridgeMoveSpeed to a distance of distanceBridgeSectionToMove after the other finished
    private System.Collections.IEnumerator MoveBridgeSections()
    {
        isMoving = true;
        for (int i = 0; i < bridgeSections.Length; i++)
        {
            GameObject section = bridgeSections[i];
            float movedDistance = 0f;
            while (movedDistance < distanceBridgeSectionToMove - 0.02f * i)
            {
                float moveStep = bridgeMoveSpeed * Time.deltaTime;
                section.transform.Translate(bridgeDirection.normalized * moveStep, Space.World);
                movedDistance += moveStep;
                yield return null;
            }
        }
        isBridgeOpen = true;
        isMoving = false;
    }

    public void CloseBridge()
    {
        if (isBridgeOpen && !isMoving)
        {
            StartCoroutine(MoveBridgeSectionsBack());
        }
    }

    // Move a bridge section back at bridgeMoveSpeed to its original position after the other finished
    private System.Collections.IEnumerator MoveBridgeSectionsBack()
    {
        isMoving = true;
        GameObject[] reversedSections = (GameObject[])bridgeSections.Clone();
        System.Array.Reverse(reversedSections);

        for (int i = 0; i < reversedSections.Length; i++)
        {
            GameObject section = reversedSections[i];
            float movedDistance = 0f;
            while (movedDistance < distanceBridgeSectionToMove - 0.02f * (reversedSections.Length - i - 1))
            {
                float moveStep = bridgeMoveSpeed * Time.deltaTime;
                section.transform.Translate(-bridgeDirection.normalized * moveStep, Space.World);
                movedDistance += moveStep;
                yield return null;
            }
        }
        isBridgeOpen = false;
        isMoving = false;
    }

    public void ToggleBridge()
    {
        if (isMoving) return;
        if (isBridgeOpen)
        {
            CloseBridge();
        }
        else
        {
            OpenBridge();
        }
    }
}
