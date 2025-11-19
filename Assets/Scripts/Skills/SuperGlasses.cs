using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SuperGlasses : ASkills
{
    [SerializeField] private GameObject superGlassesObject;
    [SerializeField] private Light superGlassesLight;
    [SerializeField] private float glassesCooldown = 0.5f;
    private float lastGlassesTime = -Mathf.Infinity;

    private bool isGlassesOn = false;

    private void Start()
    {
        if (superGlassesObject != null)
        {
            superGlassesObject.SetActive(false);
        }
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = false;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if(lastGlassesTime > 0)
            lastGlassesTime -= Time.deltaTime;
    }

    public override void MainAction()
    {
        if (lastGlassesTime > 0) return;
        lastGlassesTime = glassesCooldown;
        // Implementation for SuperGlasses main action
        ToggleGlasses();
    }

    public override void SecondaryAction()
    {
        // Implementation for SuperGlasses secondary action
    }

    private void ToggleGlasses()
    {
        isGlassesOn = !isGlassesOn;
        StartCoroutine(MoveGlasses(isGlassesOn ? 0f : -90f));
    }

    IEnumerator MoveGlasses(float degree)
    {
        float time = 0f;
        float duration = 0.3f;
        Quaternion initialRotation = superGlassesObject.transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(degree, 0f, 0f);

        while (time < duration)
        {
            superGlassesObject.transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        superGlassesObject.transform.localRotation = targetRotation;
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = isGlassesOn;
        }
    }

    public override ISkills ActivateSkill()
    {
        Debug.Log("SuperGlasses Activated");
        base.ActivateSkill();
        superGlassesObject.SetActive(true);
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = isGlassesOn;
        }

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        superGlassesObject.SetActive(false);
        superGlassesLight.enabled = false;
        if (isGlassesOn)
            ToggleGlasses();

        return this;
    }
}
