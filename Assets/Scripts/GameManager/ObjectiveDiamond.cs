using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class ObjectiveDiamond : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject diamondModel;
    [SerializeField] private ParticleSystem stealEffect;

    [Header("Events")]
    public UnityEvent OnDiamondStolen;

    private NetworkVariable<bool> isStolen = new NetworkVariable<bool>(false);
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>() ?? gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) isStolen.Value = false;
        isStolen.OnValueChanged += OnStolenStateChanged;
        UpdateVisuals(isStolen.Value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isStolen.Value || !other.CompareTag("Player")) return;
        StealDiamond();
    }

    private void StealDiamond()
    {
        isStolen.Value = true;
        if (GameStateManager.Instance != null) GameStateManager.Instance.ObjectiveStolenServerRpc();
        OnDiamondStolen?.Invoke();
    }

    private void OnStolenStateChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals(newValue);
        if (newValue && stealEffect != null) stealEffect.Play();
    }

    private void UpdateVisuals(bool stolen)
    {
        diamondModel.SetActive(!stolen);
        triggerCollider.enabled = !stolen;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetDiamondServerRpc()
    {
        isStolen.Value = false;
        triggerCollider.enabled = true;
    }
}