using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class ObjectiveDiamond : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject diamondModel;
    [SerializeField] private ParticleSystem stealEffect;
    [SerializeField] private AudioClip stealSound;

    [Header("Events")]
    public UnityEvent OnDiamondStolen;

    private NetworkVariable<bool> isStolen = new NetworkVariable<bool>(false);
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isStolen.Value = false;
            diamondModel.SetActive(true);
        }

        isStolen.OnValueChanged += OnStolenStateChanged;
        UpdateVisuals(isStolen.Value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isStolen.Value) return;

        if (other.CompareTag("Player"))
        {
            StealDiamond();
        }
    }

    private void StealDiamond()
    {
        isStolen.Value = true;

        // Notificar al GameStateManager
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ObjectiveStolenServerRpc();
        }

        OnDiamondStolen?.Invoke();
    }

    private void OnStolenStateChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals(newValue);

        if (newValue)
        {
            // Efectos visuales y de sonido
            if (stealEffect != null) stealEffect.Play();
            // AudioSource.PlayClipAtPoint(stealSound, transform.position);
        }
    }

    private void UpdateVisuals(bool stolen)
    {
        diamondModel.SetActive(!stolen);

        if (stolen)
        {
            triggerCollider.enabled = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetDiamondServerRpc()
    {
        isStolen.Value = false;
        triggerCollider.enabled = true;
    }
}