using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.Netcode; 

public class SkyWalker : ASkills
{
    [SerializeField] private float backwardForcePush = 5f;
    [Header("Glass Movement")]
    [SerializeField] private float hopForce = 10f;
    [SerializeField] private float hopDuration = 0.1f;
    [SerializeField] private float hopCooldown = 0.2f;

    private bool isAgainstGlass = false;
    private bool isHopping = false;
    private bool isDetaching = false;
    
    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    
    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        playerInput = GetComponentInParent<PlayerInput>();
    }

    private void SubscribeInputs()
    {
        if (playerInput == null) return;
        var moveAction = playerInput.actions["Move"];
        if (moveAction != null)
        {
            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
        }
    }

    private void UnsubscribeInputs()
    {
        if (playerInput == null) return;
        var moveAction = playerInput.actions["Move"];
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    private void LateUpdate()
    {
        // SÉCURITÉ RÉSEAU
        if (!IsOwner || !isActive) return;

        if (!isAgainstGlass || isHopping) return;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            StartCoroutine(HopRoutine());
        }
        else
        {
            // On force la vélocité à 0 pour coller à la vitre
            // C'est de la physique locale, le NetworkTransform sync le résultat
            playerRigidbody.linearVelocity = Vector3.zero;
        }
    }

    // ... (HopRoutine reste identique car physique locale) ...
    private IEnumerator HopRoutine()
    {
        isHopping = true;
        Vector3 moveDirection = (transform.up * moveInput.y + transform.right * moveInput.x).normalized;
        moveDirection.z = 0;
        playerRigidbody.AddForce(moveDirection * hopForce, ForceMode.Impulse);
        yield return new WaitForSeconds(hopDuration);
        if (isAgainstGlass) playerRigidbody.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(hopCooldown);
        isHopping = false;
    }

    public override void MainAction()
    {
        if (!IsOwner) return; // Sécurité
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
        if (!IsOwner || !isActive) return;

        // Logique de Raycast locale
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1f) && status)
        {
            if (hit.collider.CompareTag("Glass"))
            {
                if (isAgainstGlass != status)
                {
                    isAgainstGlass = status;
                    OnAgainstGlassChanged();
                }
            }
        } 
        else if (!status) 
        {
            if (isAgainstGlass != status)
            {
                isAgainstGlass = status;
                OnAgainstGlassChanged();
            }
        }
    }

    private void OnAgainstGlassChanged()
    {
        // Ces changements (Gravité, Enabled) doivent rester LOCAUX.
        // On ne veut pas désactiver le NetworkTransform, juste la physique locale.
        if (isAgainstGlass)
        {
            playerRigidbody.useGravity = false;
            playerController.enabled = false; // Désactive le mouvement standard
        }
        else
        {
            playerRigidbody.useGravity = true;
            if (!isDetaching) playerController.enabled = true;
        }
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        if (IsOwner) SubscribeInputs();
        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        
        if (IsOwner) UnsubscribeInputs();
        return this;
    }
    
}