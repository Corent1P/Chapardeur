using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class InteractableElement : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private float interactionTolerance = 0.5f;
    [SerializeField] private GameObject textContainer;
    [SerializeField] private bool isSingleUse = false;
    
    [Header("Événements")]
    public UnityEvent onInteract;

    private SphereCollider triggerCollider;
    private Camera mainCamera;
    
    private Dictionary<PlayerInput, System.Action<InputAction.CallbackContext>> playerListeners 
        = new Dictionary<PlayerInput, System.Action<InputAction.CallbackContext>>();

    void Start()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        triggerCollider.radius = radius; 

        if (textContainer != null)
            textContainer.SetActive(false);

        mainCamera = Camera.main;
    }

    void Update()
    {
        if (playerListeners.Count > 0)
        {
            HandleBillboard();
        }
    }

    void HandleBillboard()
    {
        if (textContainer != null && mainCamera != null)
        {
            textContainer.transform.rotation = Quaternion.LookRotation(textContainer.transform.position - mainCamera.transform.position);
        }
    }

    private void ValidateAndInteract(PlayerInput player)
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= radius + interactionTolerance)
        {
            Interact();
        }
    }

    void Interact()
    {
        onInteract.Invoke();
        
        if (isSingleUse)
        {
            CleanupAllListeners();
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerInput = other.GetComponentInParent<PlayerInput>();
            
            if (playerInput != null && !playerListeners.ContainsKey(playerInput))
            {
                var jumpAction = playerInput.actions["Jump"];
                if (jumpAction != null)
                {
                    System.Action<InputAction.CallbackContext> actionCallback = ctx => ValidateAndInteract(playerInput);
                    
                    jumpAction.performed += actionCallback;
                    
                    playerListeners.Add(playerInput, actionCallback);
                    
                    if (textContainer != null) textContainer.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerInput = other.GetComponentInParent<PlayerInput>();
            
            if (playerInput != null && playerListeners.ContainsKey(playerInput))
            {
                UnsubscribePlayer(playerInput);

                if (playerListeners.Count == 0 && textContainer != null) 
                    textContainer.SetActive(false);
            }
        }
    }

    private void UnsubscribePlayer(PlayerInput player)
    {
        var action = player.actions["Jump"];
        if (action != null && playerListeners.TryGetValue(player, out var callback))
        {
            action.performed -= callback;
        }
        playerListeners.Remove(player);
    }

    private void CleanupAllListeners()
    {
        foreach (var entry in playerListeners)
        {
            var action = entry.Key.actions["Jump"];
            if (action != null)
            {
                action.performed -= entry.Value;
            }
        }
        playerListeners.Clear();
    }

    private void OnDisable()
    {
        CleanupAllListeners();
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius + interactionTolerance);
    }
}