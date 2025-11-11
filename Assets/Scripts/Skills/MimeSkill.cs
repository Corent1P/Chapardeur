using NUnit.Framework.Internal;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;


public class MimeSkill : ASkills
{
    private List<GameObject> hitObjects;
    private GameObject previousHitObject;
    private Color originalColor;
    private bool colorSaved = false;
    private SphereCollider sphereCollider;

    void Start()
    {
        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.center = new Vector3(0, gameObject.transform.localScale.y / 2, 0);
        sphereCollider.radius = 2f;
        sphereCollider.isTrigger = true;
        sphereCollider.enabled = true;
    }

    public override void MainAction()
    {
        
    }

    public override void SecondaryAction()
    {
        Debug.Log("Skill1 Secondary Action Activated");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Object"))
            return;
        hitObjects.Add(other.gameObject);
        Renderer renderer = other.gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalColor = renderer.material.color;
            colorSaved = true;
            HighlightObject(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Object"))
            return;
        ResetObjectColor(other.gameObject);
    }

    //void Update()
    //{
    //    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out RaycastHit hitInfo, 10f))
    //    {
    //        hitObject = hitInfo.collider.gameObject;

    //        if (hitObject != previousHitObject)
    //        {
    //            if (previousHitObject != null)
    //            {
    //                Debug.Log("Resetting color of " + previousHitObject.name);
    //                ResetObjectColor(previousHitObject);
    //            }

    //            Renderer renderer = hitObject.GetComponent<Renderer>();
    //            if (renderer != null)
    //            {
    //                originalColor = renderer.material.color;
    //                colorSaved = true;
    //                HighlightObject(hitObject);
    //            }

    //            previousHitObject = hitObject;
    //        }
    //    }
    //    else
    //    {
    //        if (previousHitObject != null)
    //        {
    //            Debug.Log("Resetting color of " + previousHitObject.name);
    //            ResetObjectColor(previousHitObject);
    //            previousHitObject = null;
    //            colorSaved = false;
    //        }
    //    }
    //}

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
        GameObject targetObj = hitObjects.Find(o => o == obj);
        Renderer targetRenderer = obj.GetComponent<Renderer>();
        if (targetObj != null)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = targetRenderer.material.color;
            }
        }
    }
}
