using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SizeShifter : ASkills 
{
    [Header("Size Settings")]
    [SerializeField] private Vector3 smallSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 largeSize = new Vector3(1f, 1f, 1f);
    
    [Header("Cooldown")]
    [SerializeField] private float shiftCooldown = 0.5f;
    private float lastShiftTime = -Mathf.Infinity;

    // État synchronisé : 0 = Small, 1 = Normal, 2 = Large
    // On garde la permission Everyone pour que tout le monde puisse lire l'état actuel
    private NetworkVariable<int> netSizeState = new NetworkVariable<int>(
        1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private bool isLocked = false;
    private PlayerController playerController;

    // On garde une variable locale pour savoir d'où on vient (utile pour les animations)
    // Synchronisée avec la NetVar
    private int currentLocalState = 1; 

    private void Start()
    {
        // Initialisation comme dans votre script original
        // Note: En réseau, Start() s'exécute avant OnNetworkSpawn
        normalSize = transform.localScale;
        
        // Sécurité si les valeurs ne sont pas définies dans l'inspecteur
        if (smallSize == Vector3.one) smallSize = normalSize * 0.5f;
        if (largeSize == Vector3.one) largeSize = normalSize * 2f;

        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found in parent objects.");
        }
    }

    public override void OnNetworkSpawn()
    {
        // On s'abonne au changement de taille
        netSizeState.OnValueChanged += OnSizeStateChanged;

        // On force l'état initial sans animation (pour être raccord dès l'apparition)
        currentLocalState = netSizeState.Value;
        ApplySizeInstant(currentLocalState);
    }

    public override void OnNetworkDespawn()
    {
        netSizeState.OnValueChanged -= OnSizeStateChanged;
    }

    private void Update()
    {
        // Gestion du cooldown locale (pour l'UI ou l'input local)
        if (lastShiftTime >= 0)
            lastShiftTime -= Time.deltaTime;
    }

    // --- LOGIQUE INPUT (CLIENT/OWNER) ---

    public override void MainAction()
    {
        if (!IsOwner || isLocked) return;
        if (lastShiftTime > 0) return;

        // Logique originale : Bascule entre Small et Large
        // Si on est Small (0) -> On devient Large (2)
        // Sinon (Normal ou Large) -> On devient Small (0)
        int targetState = (netSizeState.Value == 0) ? 2 : 0;
        
        RequestSizeChangeServerRpc(targetState);
        lastShiftTime = shiftCooldown;
    }

    public override void SecondaryAction()
    {
        if (!IsOwner || isLocked) return;
        if (lastShiftTime > 0) return;

        // Logique originale : Retour à la normale si on n'y est pas
        if (netSizeState.Value != 1)
        {
            RequestSizeChangeServerRpc(1);
        }
        lastShiftTime = shiftCooldown;
    }

    // --- LOGIQUE RÉSEAU ---

    [ServerRpc]
    private void RequestSizeChangeServerRpc(int newState)
    {
        // Le serveur valide et change la variable, ce qui déclenchera l'event chez tout le monde
        netSizeState.Value = newState;
    }

    private void OnSizeStateChanged(int previousState, int newState)
    {
        // C'est ici que se joue l'animation sur TOUS les clients
        StopAllCoroutines(); // On arrête toute animation en cours pour éviter les conflits

        Vector3 targetScale = GetVectorForState(newState);

        // Décision de l'animation basée sur votre logique originale
        // SetSmallSize utilisait ScaleDownTo
        // SetLargeSize utilisait ScaleUpTo
        // SetNormalSize utilisait ScaleUpTo (si on venait de Small) ou ScaleDownTo (si on venait de Large)

        if (newState == 0) // Vers Small
        {
            StartCoroutine(ScaleDownTo(targetScale));
            if(IsOwner) ApplyPhysicsModifiers(1.8f, 1.5f); // Speed 1.8, Jump 1.5
        }
        else if (newState == 2) // Vers Large
        {
            StartCoroutine(ScaleUpTo(targetScale));
            if(IsOwner) ApplyPhysicsModifiers(0.75f, 0.9f); // Speed 0.75, Jump 0.9
        }
        else // Vers Normal (1)
        {
            // Si on vient de Small (0), on doit grandir (ScaleUp)
            // Si on vient de Large (2), on doit rétrécir (ScaleDown)
            if (previousState == 0)
                StartCoroutine(ScaleUpTo(targetScale));
            else
                StartCoroutine(ScaleDownTo(targetScale));

            if(IsOwner) ApplyPhysicsModifiers(1f, 1f); // Reset stats
        }

        currentLocalState = newState;
    }

    // --- ANIMATIONS & UTILITAIRES ---

    private Vector3 GetVectorForState(int state)
    {
        switch (state)
        {
            case 0: return smallSize;
            case 2: return largeSize;
            default: return normalSize;
        }
    }

    private void ApplySizeInstant(int state)
    {
        transform.localScale = GetVectorForState(state);
        
        if (IsOwner && playerController != null)
        {
            switch (state)
            {
                case 0: ApplyPhysicsModifiers(1.8f, 1.5f); break;
                case 2: ApplyPhysicsModifiers(0.75f, 0.9f); break;
                default: ApplyPhysicsModifiers(1f, 1f); break;
            }
        }
    }

    private void ApplyPhysicsModifiers(float speed, float jump)
    {
        if (playerController != null)
        {
            playerController.SetSpeedFactor(speed);
            playerController.SetJumpFactor(jump);
        }
    }

    // Coroutine originale restaurée (Lerp simple)
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

    // Coroutine originale restaurée (Effet de rebond/overshoot)
    IEnumerator ScaleUpTo(Vector3 targetSize)
    {
        Vector3 initialSize = transform.localScale;
        // Étape 1 : Moyenne
        transform.localScale = (targetSize + initialSize) / 2f;
        yield return new WaitForSeconds(0.1f);
        // Étape 2 : Overshoot léger
        transform.localScale = (targetSize * 2 + initialSize) / 2f;
        yield return new WaitForSeconds(0.1f);
        // Final
        transform.localScale = targetSize;
    }

    // --- GESTION DES LOCKS ---

    public void LockSize()
    {
        if (!isActive) return;
        isLocked = true;
        isSkillLocked = true; // Pour bloquer aussi le changement de skill dans SkillManager
    }

    public void UnlockSize()
    {
        if (!isActive) return;
        isLocked = false;
        isSkillLocked = false;
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        
        // Reset local au démarrage du skill
        if (IsOwner)
        {
            ApplyPhysicsModifiers(1f, 1f);
            // On demande au serveur de remettre à la normale si besoin
            if (netSizeState.Value != 1)
            {
                RequestSizeChangeServerRpc(1);
            }
        }
        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        
        if (IsOwner)
        {
            ApplyPhysicsModifiers(1f, 1f);
            // Optionnel : Forcer le retour à la normale en quittant le skill
            if (netSizeState.Value != 1)
            {
                RequestSizeChangeServerRpc(1);
            }
        }
        // Visuellement on remet instantanément pour éviter les bugs graphiques
        transform.localScale = normalSize;
        return this;
    }
}