using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private float speedFactor = 1f;
    public float jumpForce = 5f;
    private float jumpFactor = 1f;

    private Rigidbody rb;
    private PlayerInputs inputActions;
    private Vector2 moveInput;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputs();
    }

    private void OnEnable()
    {
        inputActions.PlayerControls.Enable();
        inputActions.PlayerControls.Move.performed += ctx => moveInput = -ctx.ReadValue<Vector2>();
        inputActions.PlayerControls.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.PlayerControls.Jump.performed += ctx => Jump();
    }

    private void OnDisable()
    {
        inputActions.PlayerControls.Disable();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
            transform.rotation = Quaternion.LookRotation(moveDirection);

            Vector3 targetVelocity = moveDirection * moveSpeed * speedFactor;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce * jumpFactor, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Détection sol simplifiée (par contact)
        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                isGrounded = true;
            }
        }
    }

    public void SetSpeedFactor(float newSpeedFactor)
    {
        speedFactor = newSpeedFactor;
    }

    public void SetJumpFactor(float newJumpFactor)
    {
        jumpFactor = newJumpFactor;
    }
}