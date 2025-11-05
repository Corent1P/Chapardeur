using System.Buffers.Text;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;


public class MimeSkill : MonoBehaviour
{
    private GameObject hitObject;
    private GameObject previousHitObject;
    private Color originalColor;
    private bool colorSaved = false;

    void Start()
    {
    }

    void Update()
    {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out RaycastHit hitInfo, 10f))
        {
            hitObject = hitInfo.collider.gameObject;

            if (hitObject != previousHitObject)
            {
                if (previousHitObject != null)
                {
                    Debug.Log("Resetting color of " + previousHitObject.name);
                    ResetObjectColor(previousHitObject);
                }

                Renderer renderer = hitObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    originalColor = renderer.material.color;
                    colorSaved = true;
                    HighlightObject(hitObject);
                }

                previousHitObject = hitObject;
            }
        }
        else
        {
            if (previousHitObject != null)
            {
                Debug.Log("Resetting color of " + previousHitObject.name);
                ResetObjectColor(previousHitObject);
                previousHitObject = null;
                colorSaved = false;
            }
        }
    }

    private void HighlightObject(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
    }

    private void ResetObjectColor(GameObject obj)
    {
        if (obj != null && colorSaved)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10f);
    }
}
