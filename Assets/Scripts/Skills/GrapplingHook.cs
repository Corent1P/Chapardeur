using UnityEngine;

public class GrapplingHook : ASkills
{
    [Header("Grappling Settings")]
    [SerializeField] private Material grapplingPointMaterial;
    [SerializeField] [Range(0f, 30f)] private float hookRange = 15f;
    private Transform playerTransform;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 60f; // Angle maximum en degrés
    [SerializeField] private float grapplingCooldown = 0.5f;
    private float lastGrappleTime = -Mathf.Infinity;

    [Header("Rope Settings")]
    [SerializeField] private LineRenderer ropeRenderer;
    [SerializeField] private Transform ropeOrigin;
    [SerializeField] private float maxRopeLength = 20f; // Longueur maximale de la corde
    [SerializeField] private float ropeTensionForce = 10f; // Force appliquée quand la corde est tendue
    [SerializeField] private int ropeSegments = 15; // Nombre de segments pour la courbe de la corde
    [SerializeField] private float ropeWaveAmount = 0.5f; // Amplitude de la courbe de la corde
    
    private bool hasGrabbed = false;
    private GameObject currentSelectedPoint;
    private Material originalMaterial;
    private Rigidbody playerRigidbody;
    private Vector3 grapplePoint; // Position du point d'ancrage

    private void Start()
    {
        playerTransform = GetComponentInParent<Transform>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform n'est pas assigné dans le GrapplingHook!");
        }
        
        if (playerRigidbody == null)
        {
            Debug.LogWarning("PlayerRigidbody n'est pas trouvé dans le GrapplingHook!");
        }
        
        // Configurer le LineRenderer
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
            ropeRenderer.positionCount = 0;
            Debug.Log($"GrapplingHook: LineRenderer configuré sur l'objet {ropeRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("GrapplingHook: Aucun LineRenderer assigné!");
        }
        
        // Vérifier s'il y a d'autres LineRenderers actifs
        LineRenderer[] allLineRenderers = GetComponentsInChildren<LineRenderer>(true);
        if (allLineRenderers.Length > 1)
        {
            Debug.LogWarning($"ATTENTION: {allLineRenderers.Length} LineRenderers trouvés dans la hiérarchie du GrapplingHook!");
            for (int i = 0; i < allLineRenderers.Length; i++)
            {
                Debug.Log($"  - LineRenderer {i}: {allLineRenderers[i].gameObject.name}");
            }
        }
    }

    private void Update()
    {
        if (!isActive)
            return;
        if (lastGrappleTime > 0)
            lastGrappleTime -= Time.deltaTime;
        if (hasGrabbed)
            UpdateRope();
        else
            FindBestGrapplingPoint();
    }
    
    private void FixedUpdate()
    {
        if (!isActive || !hasGrabbed)
            return;
            
        ApplyRopeTension();
    }
    
    private void UpdateRope()
    {
        if (ropeRenderer == null || ropeOrigin == null)
            return;

        DrawRope();
    }
    
    private void DrawRope()
    {
        if (ropeRenderer == null || ropeOrigin == null)
            return;

        float ropeLength = Vector3.Distance(ropeOrigin.position, grapplePoint);
        ropeRenderer.positionCount = ropeSegments;

        Vector3 startPoint = ropeOrigin.position;
        Vector3 endPoint = grapplePoint;

        for (int i = 0; i < ropeSegments; i++)
        {
            float t = i / (float)(ropeSegments - 1);
            Vector3 position = Vector3.Lerp(startPoint, endPoint, t);

            // Ajouter une courbe naturelle à la corde (effet de gravité)
            float curveAmount = ropeWaveAmount * ropeLength;
            float curve = Mathf.Sin(t * Mathf.PI) * curveAmount;
            
            // Appliquer la courbe vers le bas (gravité)
            position.y -= curve;

            ropeRenderer.SetPosition(i, position);
        }
    }
    
    private void ApplyRopeTension()
    {
        if (playerRigidbody == null || !hasGrabbed)
            return;

        Vector3 playerPosition = playerTransform.position;
        float currentDistance = Vector3.Distance(playerPosition, grapplePoint);

        // Si le joueur dépasse la longueur maximale de la corde
        if (currentDistance > maxRopeLength)
        {
            // Calculer la direction vers le point d'ancrage
            Vector3 directionToAnchor = (grapplePoint - playerPosition).normalized;
            
            // Calculer la force proportionnelle à la distance dépassée
            float overshoot = currentDistance - maxRopeLength;
            float forceMagnitude = ropeTensionForce * overshoot;

            // Appliquer la force vers le point d'ancrage
            playerRigidbody.AddForce(directionToAnchor * forceMagnitude, ForceMode.Force);
            
            // Debug visuel
            Debug.DrawLine(playerPosition, grapplePoint, Color.red);
        }
        else
        {
            Debug.DrawLine(playerPosition, grapplePoint, Color.green);
        }
    }

    private void FindBestGrapplingPoint()
    {
        if (playerTransform == null) return;

        // Trouver tous les points de grappin dans la scène
        GameObject[] grapplingPoints = GameObject.FindGameObjectsWithTag("Grappling Point");
        
        GameObject bestPoint = null;
        float minDistance = float.MaxValue;

        // Position du joueur (en ignorant la hauteur pour les calculs)
        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        // Direction dans laquelle le joueur regarde (en ignorant la hauteur)
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        foreach (GameObject point in grapplingPoints)
        {
            // Position du point (en ignorant la hauteur)
            Vector3 pointPos2D = new Vector3(point.transform.position.x, 0, point.transform.position.z);
            
            // Vérifier si le point est dans le rayon
            float distanceToPlayer = Vector3.Distance(playerPos2D, pointPos2D);
            if (distanceToPlayer > hookRange)
                continue;

            // Calculer la distance du point à la ligne de visée du joueur
            Vector3 toPoint = pointPos2D - playerPos2D;
            
            // Calculer l'angle entre la direction du joueur et la direction vers le point
            float angleToPoint = Vector3.Angle(playerForward2D, toPoint);
            
            // Si le point est en dehors de l'angle de vue, l'ignorer
            if (angleToPoint > maxAngle)
                continue;
            
            // Projection du vecteur toPoint sur la direction du joueur
            float projectionLength = Vector3.Dot(toPoint, playerForward2D);
            
            // Si le point est derrière le joueur, l'ignorer (normalement déjà géré par l'angle)
            if (projectionLength < 0)
                continue;

            // Point le plus proche sur la ligne de visée
            Vector3 closestPointOnLine = playerPos2D + playerForward2D * projectionLength;
            
            // Distance perpendiculaire du point à la ligne de visée
            float perpendicularDistance = Vector3.Distance(pointPos2D, closestPointOnLine);

            // Garder le point le plus proche de la ligne de visée
            if (perpendicularDistance < minDistance)
            {
                minDistance = perpendicularDistance;
                bestPoint = point;
            }
        }

        UpdateSelectedPoint(bestPoint);
    }

    private void UpdateSelectedPoint(GameObject newPoint)
    {
        if (currentSelectedPoint == newPoint)
            return;

        if (currentSelectedPoint != null)
        {
            Renderer renderer = currentSelectedPoint.GetComponent<Renderer>();
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
            }
        }
        currentSelectedPoint = newPoint;

        if (currentSelectedPoint != null)
        {
            Renderer renderer = currentSelectedPoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                renderer.material = grapplingPointMaterial;
            }
        }
        else
        {
            originalMaterial = null;
        }
    }

    private void StartGrapple()
    {
        if (currentSelectedPoint == null || hasGrabbed)
            return;

        hasGrabbed = true;
        grapplePoint = currentSelectedPoint.transform.position;

        Debug.Log($"Grappin lancé vers: {currentSelectedPoint.name} à {Vector3.Distance(playerTransform.position, grapplePoint):F2}m");

        // Activer le LineRenderer
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = true;
            ropeRenderer.positionCount = ropeSegments;
        }
    }

    private void StopGrapple()
    {
        if (!hasGrabbed)
            return;

        hasGrabbed = false;

        Debug.Log("Grappin relâché");

        // Désactiver la ligne de la corde
        if (ropeRenderer != null)
        {
            ropeRenderer.enabled = false;
            ropeRenderer.positionCount = 0;
        }
    }

    public override void MainAction()
    {
        if (lastGrappleTime > 0)
            return;
        lastGrappleTime = grapplingCooldown;
        if (hasGrabbed)
            StopGrapple();
        else
            if (currentSelectedPoint != null)
            {
                StartGrapple();
            }
            else
            {
                Debug.Log("Aucun point de grappin à portée");
            }
    }

    private void OnDisable()
    {
        StopGrapple();
        UpdateSelectedPoint(null);
    }

    // Optionnel: Visualiser la portée et la ligne de visée dans l'éditeur
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
        if (hasGrabbed && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(grapplePoint, maxRopeLength);
            
            // Indiquer si le joueur dépasse la longueur
            float currentDistance = Vector3.Distance(playerTransform.position, grapplePoint);
            if (currentDistance > maxRopeLength)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(playerTransform.position, grapplePoint);
            }
        }
    }
}
