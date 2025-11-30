using UnityEngine;
using Unity.Netcode;

public class GrapplingHook : ASkills
{
    [Header("Grappling Settings")]
    [SerializeField] private GameObject grappleGun;
    [SerializeField] private Material grapplingPointMaterial;
    [SerializeField] [Range(0f, 30f)] private float hookRange = 15f;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 60f;
    [SerializeField] private float grapplingCooldown = 0.5f;
    
    [Header("Rope Settings")]
    [SerializeField] private LineRenderer ropeRenderer;
    [SerializeField] private Transform ropeOrigin;
    [SerializeField] private float ropeSpringStiffness = 50f;
    [SerializeField] private float ropeDamping = 0.8f;
    [SerializeField] private float gravityCounterFactor = 0.3f;
    [SerializeField] private int ropeSegments = 15;
    [SerializeField] private float ropeWaveAmount = 0.5f;

    private NetworkVariable<bool> netIsGrappling = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> netGrapplePoint = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private Transform playerTransform;
    private float lastGrappleTime = -Mathf.Infinity;
    private float maxRopeLength;
    private bool hasGrabbedLocal = false;
    private GameObject currentSelectedPoint;
    private Material originalMaterial;
    private Rigidbody playerRigidbody;
    private PlayerController playerController;

    private void Start()
    {
        playerTransform = GetComponentInParent<Transform>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        playerController = GetComponentInParent<PlayerController>();
        
        if (grappleGun != null) grappleGun.SetActive(false);
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
            ropeRenderer.positionCount = 0;
        }
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
            if (current) ropeRenderer.positionCount = ropeSegments;
            else ropeRenderer.positionCount = 0;
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
            FindBestGrapplingPoint();
        }
    }
    
    private void FixedUpdate()
    {
        if (!isActive || !IsOwner || !netIsGrappling.Value)
            return;
            
        ApplyRopeTension(netGrapplePoint.Value);
    }
    
    private void DrawRope(Vector3 targetPoint)
    {
        if (ropeRenderer == null || ropeOrigin == null) return;

        float ropeLength = Vector3.Distance(ropeOrigin.position, targetPoint);
        Vector3 startPoint = ropeOrigin.position;

        for (int i = 0; i < ropeSegments; i++)
        {
            float t = i / (float)(ropeSegments - 1);
            Vector3 position = Vector3.Lerp(startPoint, targetPoint, t);

            float curveAmount = ropeWaveAmount * ropeLength;

            if(IsOwner && hasGrabbedLocal) 
            {
                 float tensionFactor = Mathf.Clamp01(Vector3.Distance(playerTransform.position, targetPoint) / maxRopeLength);
                 curveAmount *= 1f - tensionFactor;
            }
            
            float curve = Mathf.Sin(t * Mathf.PI) * curveAmount;
            position.y -= curve;
            ropeRenderer.SetPosition(i, position);
        }
    }
    
    private void ApplyRopeTension(Vector3 anchorPoint)
    {
        if (playerRigidbody == null) return;

        Vector3 playerPosition = playerTransform.position;

        Vector3 anchorPhysics = new Vector3(anchorPoint.x, playerPosition.y + 0.2f, anchorPoint.z);
        
        float currentDistance = Vector3.Distance(playerPosition, anchorPhysics);
        Vector3 directionToAnchor = (anchorPhysics - playerPosition).normalized;

        float distanceError = currentDistance - maxRopeLength;
        if (distanceError > 0)
        {
            float springForce = ropeSpringStiffness * distanceError;
            playerRigidbody.AddForce(directionToAnchor * springForce, ForceMode.Force);
        }

        Vector3 velocityTowardsAnchor = Vector3.Project(playerRigidbody.linearVelocity, directionToAnchor);
        float dampingForce = -ropeDamping * velocityTowardsAnchor.magnitude;
        playerRigidbody.AddForce(directionToAnchor * dampingForce, ForceMode.Force);

        Vector3 gravityForce = Physics.gravity * playerRigidbody.mass;
        playerRigidbody.AddForce(-gravityForce * gravityCounterFactor, ForceMode.Force);

        if (playerController != null)
        {
            float speedFactor = Mathf.Clamp01(maxRopeLength / currentDistance);
            playerController.SetSpeedFactor(speedFactor);
        }
    }

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
                StartGrappleLocal(currentSelectedPoint.transform.position);
            }
        }
    }

    private void StartGrappleLocal(Vector3 point)
    {
        hasGrabbedLocal = true;
        maxRopeLength = Vector3.Distance(playerTransform.position, point);

        RequestStartGrappleServerRpc(point);
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

    public override void SecondaryAction()
    {
        if (!IsOwner || !netIsGrappling.Value) return;

        if (transform.position.y < netGrapplePoint.Value.y)
        {
            maxRopeLength *= 0.9f;
            if (maxRopeLength < 0.1f) maxRopeLength = 0.1f;
        }
    }

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

    private void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        Vector3 forward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;
        
        // Dessiner la portée du grappin (sphère complète)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(playerTransform.position, hookRange);

        // Dessiner le cône de vision (angle de vue)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 leftBoundary = Quaternion.Euler(0, -maxAngle, 0) * forward2D;
        Vector3 rightBoundary = Quaternion.Euler(0, maxAngle, 0) * forward2D;
        
        // Lignes des limites du cône
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftBoundary * hookRange);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightBoundary * hookRange);
        
        // Arc pour visualiser le cône (approximation avec plusieurs lignes)
        int arcSegments = 20;
        Vector3 previousPoint = playerTransform.position + leftBoundary * hookRange;
        for (int i = 1; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(-maxAngle, maxAngle, i / (float)arcSegments);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward2D;
            Vector3 point = playerTransform.position + direction * hookRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Dessiner la ligne de visée centrale
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + forward2D * hookRange);

        // Dessiner le point sélectionné
        if (currentSelectedPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerTransform.position, currentSelectedPoint.transform.position);
            Gizmos.DrawWireSphere(currentSelectedPoint.transform.position, 0.5f);
        }
        
        // Dessiner la longueur maximale de la corde quand le grappin est actif
        if (netIsGrappling.Value && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(netGrapplePoint.Value, maxRopeLength);
            
            // Indiquer si le joueur dépasse la longueur
            float currentDistance = Vector3.Distance(playerTransform.position, netGrapplePoint.Value);
            if (currentDistance > maxRopeLength)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(playerTransform.position, netGrapplePoint.Value);
            }
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
        return this;
    }
}