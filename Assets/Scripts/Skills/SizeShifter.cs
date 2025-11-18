using UnityEngine;

public class SizeShifter : ASkills
{
    [SerializeField] private Vector3 smallSize = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Vector3 normalSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 largeSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private float shiftCooldown = 0.5f;
    private float lastShiftTime = -Mathf.Infinity;
    private bool isSmall = false;
    private PlayerController playerController;

    private void Start()
    {
        normalSize = transform.localScale;
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
        if (lastShiftTime > 0) return;

        if (isSmall)
            SetLargeSize();
        else
            SetSmallSize();
        lastShiftTime = shiftCooldown;
    }

    public override void SecondaryAction()
    {
        if (lastShiftTime > 0) return;

        SetNormalSize();
        lastShiftTime = shiftCooldown;
    }

    private void SetSmallSize()
    {
        transform.localScale = smallSize;
        isSmall = true;
        playerController.SetSpeedFactor(1.8f);
        playerController.SetJumpFactor(1.5f);
    }

    private void SetNormalSize()
    {
        transform.localScale = normalSize;
        isSmall = false;
        playerController.SetSpeedFactor(1f);
        playerController.SetJumpFactor(1f);
    }

    private void SetLargeSize()
    {
        transform.localScale = largeSize;
        isSmall = false;
        playerController.SetSpeedFactor(0.75f);
        playerController.SetJumpFactor(0.9f);
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
