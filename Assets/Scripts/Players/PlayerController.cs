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

    [Header("Look Settings")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundLayerMask = ~0; // Tous les layers par défaut

    private float speed = 0f;
    private Camera mainCamera;
    private Plane groundPlane;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isGrounded = true;
    private SkillManager skillManager;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponentInParent<PlayerInput>();
        skillManager = GetComponent<SkillManager>();
        mainCamera = Camera.main;
        groundPlane = new Plane(Vector3.up, Vector3.zero);
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
        AnimPlayer(moveInput);
    }

private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
            // Ne pas changer la rotation ici, c'est HandleLook qui s'en charge

            Vector3 targetVelocity = moveDirection * moveSpeed * speedFactor;

            // --- Check Camera Limits ---
            Vector3 futurePosition = rb.position + (targetVelocity * Time.fixedDeltaTime);

            if (Camera.main != null)
            {
                Vector3 viewportPos = Camera.main.WorldToViewportPoint(futurePosition);

                float margin = 0.02f;

                if (viewportPos.x < margin && targetVelocity.x < 0)
                {
                    targetVelocity.x = 0;
                }
                else if (viewportPos.x > 1 - margin && targetVelocity.x > 0)
                {
                    targetVelocity.x = 0;
                }
                if (viewportPos.y < margin && targetVelocity.z < 0)
                {
                    targetVelocity.z = 0;
                }
                else if (viewportPos.y > 1 - margin && targetVelocity.z > 0)
                {
                    targetVelocity.z = 0;
                }
            }
            // --------------------------
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
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Obtenir la position de la souris à l'écran
        Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        
        // Créer un rayon de la caméra vers la souris
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        
        // Mettre à jour le plan au niveau du joueur
        groundPlane.SetNormalAndPosition(Vector3.up, new Vector3(0, transform.position.y, 0));
        
        // Trouver le point d'intersection avec le plan
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // S'assurer qu'on ne regarde pas vers le haut/bas
            
            if (direction.sqrMagnitude > 0.01f)
            {
                // Rotation fluide vers la cible
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
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

    private void AnimPlayer(Vector2 moveInput)
    {
        if (speed == moveInput.magnitude) return;
        speed = moveInput.magnitude;
        if (skillManager != null)
        {
            skillManager.UpdateActiveSkillAnimation(speed, false);
        }
    }
}