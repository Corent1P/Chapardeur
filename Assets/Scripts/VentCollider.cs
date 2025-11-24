using System.Drawing;
using UnityEngine;

public class VentCollider : MonoBehaviour
{
    [SerializeField] private Collider ventArea;
    [SerializeField] private bool isEntranceVent = true;

    private void Awake()
    {
        if (ventArea == null)
        {
            ventArea = GetComponent<Collider>();
        }
        ventArea.isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SizeShifter sizeShifter = other.GetComponent<SizeShifter>();
            if (sizeShifter != null)
            {
                if (isEntranceVent)
                {
                    sizeShifter.LockSize();
                }
                else
                {
                    sizeShifter.UnlockSize();
                }
            }
        }
    }
}
