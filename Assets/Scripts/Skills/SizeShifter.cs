using System.Collections;
using UnityEngine;

public class SizeShifter : ASkills
{
    [SerializeField] private Vector3 smallSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 normalSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 largeSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private float shiftCooldown = 0.5f;
    private float lastShiftTime = -Mathf.Infinity;
    private bool isSmall = false;
    private bool isLocked = false;
    private PlayerController playerController;

    private void Start()
    {
        normalSize = transform.localScale;
        if (smallSize == Vector3.one)
            smallSize = normalSize * 0.5f;
        if (largeSize == Vector3.one)
            largeSize = normalSize * 2f;
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found in parent objects.");
        }
    }

    private void Update()
    {
        if (lastShiftTime >= 0)
            lastShiftTime -= Time.deltaTime;
    }

    public override void MainAction()
    {
        if (isLocked) return;
        if (lastShiftTime > 0) return;

        if (isSmall)
            SetLargeSize();
        else
            SetSmallSize();
        lastShiftTime = shiftCooldown;
    }

    public override void SecondaryAction()
    {
        if (isLocked) return;
        if (lastShiftTime > 0) return;

        if (transform.localScale != normalSize)
            SetNormalSize();
        lastShiftTime = shiftCooldown;
    }

    private void SetSmallSize()
    {
        isSmall = true;
        playerController.SetSpeedFactor(1.8f);
        playerController.SetJumpFactor(1.5f);
        StartCoroutine(ScaleDownTo(smallSize));
    }

    IEnumerator ScaleDownTo(Vector3 targetSize)
    {
        Vector3 initialSize = transform.localScale;
        float elapsedTime = 0f;
        float duration = 0.2f;

        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(initialSize, targetSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetSize;
    }

    IEnumerator ScaleUpTo(Vector3 targetSize)
    {
        Vector3 initialSize = transform.localScale;
        transform.localScale = (targetSize + initialSize) / 2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = (targetSize * 2 + initialSize) / 2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = targetSize;
    }

    private void SetNormalSize()
    {
        if (isSmall)
            StartCoroutine(ScaleUpTo(normalSize));
        else
            StartCoroutine(ScaleDownTo(normalSize));
        playerController.SetSpeedFactor(1f);
        playerController.SetJumpFactor(1f);
    }

    private void SetLargeSize()
    {
        StartCoroutine(ScaleUpTo(largeSize));
        isSmall = false;
        playerController.SetSpeedFactor(0.75f);
        playerController.SetJumpFactor(0.9f);
    }

    public void LockSize()
    {
        if (!isActive) return;
        isLocked = true;
    }

    public void UnlockSize()
    {
        if (!isActive) return;
        isLocked = false;
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        SetNormalSize();
        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        SetNormalSize();
        return this;
    }
}
