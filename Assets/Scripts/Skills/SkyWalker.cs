using UnityEngine;
using System.Collections;
using UnityEditor.Experimental.GraphView;

public class SkyWalker : ASkills
{
    [SerializeField] private float backwardForcePush = 5f;
    private bool isAgainstGlass = false;
    private bool isDetaching = false;
    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private PlayerInputs inputActions;
    private Vector2 moveInput;

    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = new PlayerInputs();
        inputActions.PlayerControls.Enable();
        inputActions.PlayerControls.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.PlayerControls.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        inputActions.PlayerControls.Disable();
    }

    private void Start()
    {
        if (inputActions == null)
            inputActions = new PlayerInputs();
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found in parent objects.");
        }

        playerRigidbody = playerController.GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogWarning("Rigidbody component not found on PlayerController.");
        }
    }

    private void LateUpdate()
    {
        if (!isActive) return;
        if (!isAgainstGlass) return;

        // Keep the player stuck to the glass surface
        Vector3 moveDirection = (transform.up * moveInput.y + transform.right * moveInput.x).normalized;
        moveDirection.z = 0;
        playerRigidbody.linearVelocity = moveDirection * (playerController.moveSpeed * 0.5f);
    }

    public override void MainAction()
    {
        if (!isAgainstGlass || isDetaching) return;
        StartCoroutine(DetachFromGlassRoutine());
    }

    private IEnumerator DetachFromGlassRoutine()
    {
        isDetaching = true;
        if (playerRigidbody != null)
        {
            Vector3 backwardForce = -playerController.transform.forward * backwardForcePush;

            SetAgainstGlass(false);
            playerRigidbody.AddForce(backwardForce, ForceMode.VelocityChange);

            yield return new WaitForSeconds(0.5f);

            if (!isAgainstGlass)
            {
                playerController.enabled = true;
            }
        }
        isDetaching = false;
    }

    public override void SecondaryAction()
    {
    }

    public void SetAgainstGlass(bool status)
    {
        if (!isActive) return;

        if (isAgainstGlass != status)
        {
            isAgainstGlass = status;
            OnAgainstGlassChanged();
        }
    }

    private void OnAgainstGlassChanged()
    {
        if (isAgainstGlass)
        {
            // playerController.SetCanJumpOnGlass(true);
            playerRigidbody.useGravity = false;
            playerController.enabled = false;
        }
        else
        {
            // playerController.SetCanJumpOnGlass(false);
            playerRigidbody.useGravity = true;
            if (!isDetaching)
            {
                playerController.enabled = true;
            }
        }
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        transform.localScale = Vector3.one * 3.0f;

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        transform.localScale = Vector3.one;

        return this;
    }
}
