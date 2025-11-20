using System.Linq;
using UnityEngine;


public class MimeSkill : ASkills
{

    [Header("Mime Skill Settings")]
    [SerializeField] private Material MimeObjectMaterial;
    [SerializeField] private float mimeRange = 3f;
    private Transform playerTransform;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 60f;
    [SerializeField] private GameObject[] MimeObjects;

    private GameObject currentSelectedObject;
    private Mesh basePlayerMesh;
    private Material basePlayerMaterial;
    private Vector3 baseplayerScale;
    private Material[] originalMaterials;
    private Material[] updatedMaterials;

    private void Update()
    {
        if (!isActive)
            return;
        else
            FindNearestMimeObject();
    }
    public override void MainAction()
    {
        MorphPlayer(currentSelectedObject);
    }

    public override void SecondaryAction()
    {
        Debug.Log("Resetting player morph");
        ResetPlayerMorph();
    }

    private void FindNearestMimeObject()
    {
        if (playerTransform == null) return;

        // Trouver tous les points de grappin dans la scène
        GameObject[] mimeObjects = GameObject.FindGameObjectsWithTag("MimeObject");

        GameObject nearestMimeObject = null;
        float minDistance = float.MaxValue;

        // Position du joueur (en ignorant la hauteur pour les calculs)
        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        // Direction dans laquelle le joueur regarde (en ignorant la hauteur)
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        foreach (GameObject obj in mimeObjects)
        {
            // Position de l'objet (en ignorant la hauteur)
            Vector3 objPos2D = new Vector3(obj.transform.position.x, 0, obj.transform.position.z);

            // Vérifier si l'objet est dans le rayon
            float distanceToPlayer = Vector3.Distance(playerPos2D, objPos2D);
            if (distanceToPlayer > mimeRange)
                continue;

            // Calculer la distance de l'objet à la ligne de visée du joueur
            Vector3 toPoint = objPos2D - playerPos2D;

            // Calculer l'angle entre la direction du joueur et la direction vers l'objet
            float angleToObject = Vector3.Angle(playerForward2D, toPoint);

            // Si l'objet est en dehors de l'angle de vue, l'ignorer
            if (angleToObject > maxAngle)
                continue;

            // Projection du vecteur toPoint sur la direction du joueur
            float projectionLength = Vector3.Dot(toPoint, playerForward2D);

            // Si le point est derrière le joueur, l'ignorer (normalement déjà géré par l'angle)
            if (projectionLength < 0)
                continue;

            // Point le plus proche sur la ligne de visée
            Vector3 closestPointOnLine = playerPos2D + playerForward2D * projectionLength;

            // Distance perpendiculaire de l'objet à la ligne de visée
            float perpendicularDistance = Vector3.Distance(objPos2D, closestPointOnLine);

            // Garder l'objet le plus proche de la ligne de visée
            if (perpendicularDistance < minDistance)
            {
                minDistance = perpendicularDistance;
                nearestMimeObject = obj;
            }
        }

        HighlightObject(nearestMimeObject);
    }

    private void HighlightObject(GameObject obj)
    {
        if (currentSelectedObject == obj)
            return;

        if (currentSelectedObject != null)
        {
            MeshRenderer renderer = currentSelectedObject.GetComponent<MeshRenderer>();
            if (renderer != null && originalMaterials.Count() > 0)
            {
                renderer.materials = originalMaterials;
            }
        }
        currentSelectedObject = obj;

        if (currentSelectedObject != null)
        {
            MeshRenderer renderer = currentSelectedObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                originalMaterials = renderer.materials;
                updatedMaterials = new Material[originalMaterials.Length + 1];
                for (int i = 0; i < originalMaterials.Length; i++) {
                    updatedMaterials[i] = originalMaterials[i];
                }
                updatedMaterials[updatedMaterials.Length - 1] = MimeObjectMaterial;
                renderer.materials = updatedMaterials;
            }
        }
        else
        {
            originalMaterials = null;
        }
    }

    private void MorphPlayer(GameObject obj)
    {
        if (obj == null)
            return;
        string objName = obj.name;
        foreach (GameObject mimeObj in MimeObjects)
        {
            if (mimeObj.name == objName)
            {
                Mesh sharedMesh = mimeObj.GetComponent<MeshFilter>().sharedMesh;
                MeshRenderer objMeshRenderer = mimeObj.GetComponent<MeshRenderer>();
                if (sharedMesh != null && objMeshRenderer != null)
                {
                    MeshFilter playerMeshFilter = GetComponent<MeshFilter>();
                    MeshRenderer playerMeshRenderer = GetComponent<MeshRenderer>();
                    MeshCollider playerMeshCollider = GetComponent<MeshCollider>();
                    if (playerMeshFilter != null && playerMeshRenderer != null && playerMeshCollider != null)
                    {
                        playerMeshFilter.sharedMesh = sharedMesh;
                        playerMeshRenderer.material = objMeshRenderer.sharedMaterial;
                        playerMeshCollider.sharedMesh = sharedMesh;
                        Vector3 objScale = mimeObj.transform.localScale;
                        transform.localScale = objScale;
                    }
                }
                break;
            }
        }
    }

    private void ResetPlayerMorph()
    {
        GetComponent<MeshFilter>().mesh = basePlayerMesh;
        GetComponent<MeshRenderer>().material = basePlayerMaterial;
        GetComponent<MeshCollider>().sharedMesh = basePlayerMesh;
        transform.localScale = baseplayerScale;
    }

    private void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        Vector3 forward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        // Dessiner la portée du grappin (sphère complète)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(playerTransform.position, mimeRange);

        // Dessiner le cône de vision (angle de vue)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 leftBoundary = Quaternion.Euler(0, -maxAngle, 0) * forward2D;
        Vector3 rightBoundary = Quaternion.Euler(0, maxAngle, 0) * forward2D;

        // Lignes des limites du cône
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftBoundary * mimeRange);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightBoundary * mimeRange);

        // Arc pour visualiser le cône (approximation avec plusieurs lignes)
        int arcSegments = 20;
        Vector3 previousPoint = playerTransform.position + leftBoundary * mimeRange;
        for (int i = 1; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(-maxAngle, maxAngle, i / (float)arcSegments);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward2D;
            Vector3 point = playerTransform.position + direction * mimeRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Dessiner la ligne de visée centrale
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + forward2D * mimeRange);

        // Dessiner le point sélectionné
        if (currentSelectedObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerTransform.position, currentSelectedObject.transform.position);
            Gizmos.DrawWireSphere(currentSelectedObject.transform.position, 0.5f);
        }
    }

    public override ISkills ActivateSkill()
    {
        base.ActivateSkill();
        playerTransform = transform;
        basePlayerMesh = GetComponent<MeshFilter>().mesh;
        basePlayerMaterial = GetComponent<MeshRenderer>().material;
        baseplayerScale = transform.localScale;
        return this;
    }

}
