using UnityEngine;
using Unity.Netcode;

public class GrapplingHook : ASkills
{
    [Header("Grappling Settings")]
    [SerializeField] private GameObject grappleGun;
    [SerializeField] private Material grapplingPointMaterial;
    [SerializeField] [Range(0f, 30f)] private float hookRange = 15f;
    [SerializeField] [Range(0f, 180f)] private float maxAngle = 60f;
    [SerializeField] private float grapplingCooldown = 0.5f;
    
    [Header("Physics & Interaction (New)")]
    [SerializeField] private float jointSpringForce = 100f; // Force du ressort
    [SerializeField] private float jointDamping = 5f;       // Amortissement
    [SerializeField] private float reelInSpeed = 10f;       // Vitesse du tiré (Secondary Action)
    [SerializeField] private float minTensionToTrigger = 5f; // Seuil min pour notifier l'objet

    [Header("Rope Settings")]
    [SerializeField] private LineRenderer ropeRenderer;
    [SerializeField] private Transform ropeOrigin;
    [SerializeField] private int ropeSegments = 15;
    [SerializeField] private float ropeWaveAmount = 0.5f;

    // --- NETWORK VARIABLES ---
    private NetworkVariable<bool> netIsGrappling = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private NetworkVariable<Vector3> netGrapplePoint = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // --- LOCAL VARIABLES ---
    private Transform playerTransform;
    private Rigidbody playerRigidbody;
    private PlayerController playerController;

    private float lastGrappleTime = -Mathf.Infinity;
    private float currentRopeLength; // La longueur actuelle de la corde
    private bool isReelingIn = false; // Est-ce qu'on tire sur la corde ?
    
    private GameObject currentSelectedPoint;
    private GrappleInteractable currentInteractable; // L'objet qu'on tient (optionnel)
    private Material originalMaterial;

    private void Start()
    {
        playerTransform = GetComponentInParent<Transform>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        playerController = GetComponentInParent<PlayerController>();
        
        if (grappleGun != null) grappleGun.SetActive(false);
        if (ropeRenderer != null) ropeRenderer.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        netIsGrappling.OnValueChanged += OnGrappleStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        netIsGrappling.OnValueChanged -= OnGrappleStateChanged;
    }

    private void OnGrappleStateChanged(bool prev, bool current)
    {
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = current;
            ropeRenderer.positionCount = current ? ropeSegments : 0;
        }

        // Si on lâche, on prévient l'objet interactif (fermeture porte etc)
        if (!current && IsOwner && currentInteractable != null)
        {
            currentInteractable.OnGrappleDetach();
            currentInteractable = null;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (lastGrappleTime > 0) lastGrappleTime -= Time.deltaTime;

        if (netIsGrappling.Value)
        {
            DrawRope(netGrapplePoint.Value);
        }
        else if (IsOwner)
        {
            FindBestGrapplingPoint(); // Ta méthode originale
        }
    }
    
    private void FixedUpdate()
    {
        if (!isActive || !IsOwner || !netIsGrappling.Value) return;

        // Reset du flag input à chaque frame physique (pour éviter qu'il reste coincé)
        // Si SecondaryAction est maintenu, il sera remis à true avant la prochaine physique
        ApplyGrapplePhysics(netGrapplePoint.Value);
        isReelingIn = false; 
    }

    // --- NOUVELLE PHYSIQUE (Tension & Boutons) ---
    private void ApplyGrapplePhysics(Vector3 anchorPoint)
    {
        if (playerRigidbody == null) return;

        Vector3 playerPos = playerTransform.position;
        float distance = Vector3.Distance(playerPos, anchorPoint);
        Vector3 direction = (anchorPoint - playerPos).normalized;

        // 1. Gestion de la longueur de corde
        if (currentRopeLength <= 0.1f) currentRopeLength = distance; // Init safe

        if (isReelingIn)
        {
            // On raccourcit la corde (on se tire vers le point)
            currentRopeLength -= reelInSpeed * Time.fixedDeltaTime;
            if (currentRopeLength < 1f) currentRopeLength = 1f;
        }
        else
        {
            // Pas de mou : si on se rapproche, la corde raccourcit
            if (distance < currentRopeLength) currentRopeLength = distance;
        }

        // 2. Calcul de la Tension (Ressort)
        float appliedForce = 0f;

        if (distance > currentRopeLength)
        {
            float stretch = distance - currentRopeLength;
            float springForce = stretch * jointSpringForce;
            
            // Unity 6: linearVelocity / Unity <6: velocity
            float velocityAlongRope = Vector3.Dot(playerRigidbody.linearVelocity, direction);
            float dampingForce = velocityAlongRope * jointDamping;

            float totalForce = springForce - dampingForce;

            if (totalForce > 0)
            {
                playerRigidbody.AddForce(direction * totalForce, ForceMode.Force);
                appliedForce = totalForce;
            }
        }

        // 3. Interaction avec l'objet (Bouton)
        if (currentInteractable != null && appliedForce > minTensionToTrigger)
        {
            currentInteractable.ApplyTension(appliedForce);
        }
    }

    // --- MAIN ACTION (Tir / Lâcher) ---
    public override void MainAction()
    {
        if (!IsOwner) return;
        if (lastGrappleTime > 0) return;
        
        lastGrappleTime = grapplingCooldown;

        if (netIsGrappling.Value)
        {
            RequestStopGrappleServerRpc();
        }
        else
        {
            if (currentSelectedPoint != null)
            {
                StartGrappleLocal(currentSelectedPoint);
            }
        }
    }

    // --- SECONDARY ACTION (Tirer la corde) ---
    public override void SecondaryAction()
    {
        if (IsOwner && netIsGrappling.Value)
        {
            isReelingIn = true;
        }
    }

    private void StartGrappleLocal(GameObject targetObj)
    {
        // On initialise la physique immédiatement
        currentRopeLength = Vector3.Distance(playerTransform.position, targetObj.transform.position);
        
        // On regarde si c'est un objet interactif
        currentInteractable = targetObj.GetComponent<GrappleInteractable>();

        RequestStartGrappleServerRpc(targetObj.transform.position);
    }

    [ServerRpc]
    private void RequestStartGrappleServerRpc(Vector3 point)
    {
        netGrapplePoint.Value = point;
        netIsGrappling.Value = true;
    }

    [ServerRpc]
    private void RequestStopGrappleServerRpc()
    {
        netIsGrappling.Value = false;
    }

    // --- TA METHODE DE DETECTION ORIGINALE (RESTORED) ---
    private void FindBestGrapplingPoint()
    {
        if (playerTransform == null) return;
        
        GameObject[] grapplingPoints = GameObject.FindGameObjectsWithTag("Grappling Point");
        GameObject bestPoint = null;
        float minDistance = float.MaxValue;

        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        foreach (GameObject point in grapplingPoints)
        {
            Vector3 pointPos2D = new Vector3(point.transform.position.x, 0, point.transform.position.z);
            float distanceToPlayer = Vector3.Distance(playerPos2D, pointPos2D);
            if (distanceToPlayer > hookRange) continue;

            Vector3 toPoint = pointPos2D - playerPos2D;
            float angleToPoint = Vector3.Angle(playerForward2D, toPoint);
            if (angleToPoint > maxAngle) continue;
            if (Vector3.Dot(toPoint, playerForward2D) < 0) continue;

            float perpDist = Vector3.Distance(pointPos2D, playerPos2D + playerForward2D * Vector3.Dot(toPoint, playerForward2D));
            if (perpDist < minDistance)
            {
                minDistance = perpDist;
                bestPoint = point;
            }
        }
        UpdateSelectedPoint(bestPoint);
    }

    private void UpdateSelectedPoint(GameObject newPoint)
    {
        if (currentSelectedPoint == newPoint) return;

        // Reset Material
        if (currentSelectedPoint != null)
        {
            Renderer renderer = currentSelectedPoint.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null) renderer.material = originalMaterial;
        }
        
        currentSelectedPoint = newPoint;

        // Set Material
        if (currentSelectedPoint != null)
        {
            Renderer renderer = currentSelectedPoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.sharedMaterial;
                renderer.material = grapplingPointMaterial;
            }
        }
        else originalMaterial = null;
    }

    private void DrawRope(Vector3 targetPoint)
    {
        if (ropeRenderer == null || ropeOrigin == null) return;

        Vector3 startPoint = ropeOrigin.position;
        // On utilise currentRopeLength pour la visualisation si possible pour voir le mou
        float effectiveLength = (currentRopeLength > 0.1f) ? currentRopeLength : Vector3.Distance(startPoint, targetPoint);
        float actualDist = Vector3.Distance(startPoint, targetPoint);

        for (int i = 0; i < ropeSegments; i++)
        {
            float t = i / (float)(ropeSegments - 1);
            Vector3 position = Vector3.Lerp(startPoint, targetPoint, t);

            // Simulation visuelle de la tension
            float tensionRatio = Mathf.Clamp01(actualDist / effectiveLength);
            float wave = (1f - tensionRatio) * ropeWaveAmount;

            float curve = Mathf.Sin(t * Mathf.PI) * wave;
            position.y -= curve;
            ropeRenderer.SetPosition(i, position);
        }
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        if (grappleGun != null) grappleGun.SetActive(true);
        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        if (grappleGun != null) grappleGun.SetActive(false);
        if(IsOwner && netIsGrappling.Value) RequestStopGrappleServerRpc();
        UpdateSelectedPoint(null);
        if (currentInteractable != null) 
        {
            currentInteractable.OnGrappleDetach();
            currentInteractable = null;
        }
        return this;
    }
}