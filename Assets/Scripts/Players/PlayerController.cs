using Unity.Netcode;
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
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponentInParent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            this.enabled = false; 
            return;
        }

        SubscribeToInputs();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
        {
            UnsubscribeFromInputs();
        }
    }

    private void SubscribeToInputs()
    {
        if (playerInput == null) return;

        var moveAction = playerInput.actions["Move"];
        var lookAction = playerInput.actions["Look"];
        var jumpAction = playerInput.actions["Jump"];

        if (moveAction != null)
        {
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }

        if (lookAction != null)
        {
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
        }

        if (jumpAction != null)
        {
            jumpAction.performed += OnJump;
        }
    }

    private void UnsubscribeFromInputs()
    {
        if (playerInput == null) return;

        var moveAction = playerInput.actions["Move"];
        var lookAction = playerInput.actions["Look"];
        var jumpAction = playerInput.actions["Jump"];

        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }

        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
        }

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnLook(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnJump(InputAction.CallbackContext ctx) => Jump();

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleLook();
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

    private void HandleLook()
    {
        if (lookInput == Vector2.zero) return;

        Vector3 moveDirection = new Vector3(lookInput.x, 0f, lookInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
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
        if (!IsOwner) return; 

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