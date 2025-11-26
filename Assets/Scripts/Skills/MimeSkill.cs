using System.Collections;
using System.Collections.Generic; // Nécessaire pour les Listes
using UnityEngine;
using UnityEngine.VFX;

public class MimeSkill : ASkills
{
    [Header("Mime Skill Settings")]
    [SerializeField] private Material MimeObjectMaterial;
    [SerializeField] private float mimeRange = 3f;
    private Transform playerTransform;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 60f;
    [SerializeField] private VisualEffect morphVFX;
    [SerializeField] private GameObject[] MimeObjects; // Liste des prefabs possibles pour le Morph

    [Header("Scale Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // --- Optimisation Variables ---
    private GameObject currentSelectedObject;
    private Renderer currentSelectedRenderer;
    private string currentMorphObjectName;

    private float morphCooldownTime = 2f;
    private float currentCooldownTime = 0f;

    private Mesh basePlayerMesh;
    private Material basePlayerMaterial;
    private Vector3 baseplayerScale;

    // Buffers pour éviter les allocations (Garbage Collection)
    private List<Material> _originalMaterialsBuffer = new List<Material>();
    private List<Material> _tempMaterialsBuffer = new List<Material>();
    
    // Buffer pour la physique (max 20 objets détectés autour du joueur, ajustable)
    private Collider[] _hitCollidersBuffer = new Collider[20]; 

    private void Start()
    {
        morphVFX = GetComponentInChildren<VisualEffect>();
        if (morphVFX != null) morphVFX.Stop();
    }

    private void Update()
    {
        if (!isActive)
            return;
        
        // Optimisation: On cherche uniquement si le cooldown est terminé ou pour mettre à jour le highlight
        FindNearestMimeObject();
        
        if (currentCooldownTime > 0f)
            currentCooldownTime -= Time.deltaTime;
    }

    public override void MainAction()
    {
        MorphPlayer(currentSelectedObject);
    }

    public override void SecondaryAction()
    {
        ResetPlayerMorph();
    }

    private void FindNearestMimeObject()
    {
        if (playerTransform == null) return;

        // 1. Optimisation Majeure : Remplacement de FindGameObjectsWithTag (très lourd) 
        // par OverlapSphereNonAlloc (très léger et localisé).
        // Cela suppose que vos objets "MimeObject" ont des Colliders.
        int hitCount = Physics.OverlapSphereNonAlloc(playerTransform.position, mimeRange, _hitCollidersBuffer);

        GameObject nearestMimeObject = null;
        float minDistance = float.MaxValue;

        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _hitCollidersBuffer[i];
            
            // Vérification du tag sans allocation mémoire
            if (!col.CompareTag("MimeObject")) 
                continue;

            Transform objTransform = col.transform;
            Vector3 objPos2D = new Vector3(objTransform.position.x, 0, objTransform.position.z);

            // La distance est déjà pré-filtrée par OverlapSphere, mais on a besoin de la valeur précise pour le tri
            // On peut utiliser SqrMagnitude pour éviter une racine carrée si on veut encore optimiser, 
            // mais gardons Distance pour la lisibilité mathématique ici.

            Vector3 toPoint = objPos2D - playerPos2D;
            float angleToObject = Vector3.Angle(playerForward2D, toPoint);

            if (angleToObject > maxAngle)
                continue;

            float projectionLength = Vector3.Dot(toPoint, playerForward2D);

            if (projectionLength < 0)
                continue;

            Vector3 closestPointOnLine = playerPos2D + playerForward2D * projectionLength;
            float perpendicularDistance = Vector3.Distance(objPos2D, closestPointOnLine);

            if (perpendicularDistance < minDistance)
            {
                minDistance = perpendicularDistance;
                nearestMimeObject = col.gameObject;
            }
        }

        HighlightObject(nearestMimeObject);
    }

    private void HighlightObject(GameObject obj)
    {
        // Si l'objet n'a pas changé, on ne fait rien
        if (currentSelectedObject == obj)
            return;

        // 1. Restaurer l'ancien objet
        if (currentSelectedObject != null && currentSelectedRenderer != null)
        {
            // On remet la liste originale sauvegardée
            // Utilisation de SetSharedMaterials pour éviter l'allocation
            if (_originalMaterialsBuffer.Count > 0)
            {
                // Note: Get/SetSharedMaterials modifie le matériau source. 
                // Si vous voulez modifier l'instance unique, utilisez GetMaterials/SetMaterials.
                // Ici, pour éviter les fuites de mémoire (Material leak), il est souvent mieux d'utiliser Shared 
                // SAUF si le highlight doit être unique par instance. 
                // Pour reproduire le comportement exact du script original (instance), j'utilise SetMaterials (copie).
                currentSelectedRenderer.SetMaterials(_originalMaterialsBuffer);
            }
        }

        currentSelectedObject = obj;
        
        // Nettoyage des buffers
        _originalMaterialsBuffer.Clear();
        _tempMaterialsBuffer.Clear();

        // 2. Appliquer sur le nouveau
        if (currentSelectedObject != null)
        {
            currentSelectedRenderer = currentSelectedObject.GetComponent<Renderer>();
            
            if (currentSelectedRenderer != null)
            {
                // Remplacer "renderer.materials" (qui alloue un array) par GetMaterials(List)
                currentSelectedRenderer.GetMaterials(_originalMaterialsBuffer);

                // Copie dans le buffer temporaire pour modification
                _tempMaterialsBuffer.AddRange(_originalMaterialsBuffer);
                _tempMaterialsBuffer.Add(MimeObjectMaterial);

                // Application sans créer de "new Material[]"
                currentSelectedRenderer.SetMaterials(_tempMaterialsBuffer);
            }
        }
        else
        {
            currentSelectedRenderer = null;
        }
    }

    private void MorphPlayer(GameObject obj)
    {
        if (obj == null || currentCooldownTime > 0f)
            return;
        
        // Optimisation string : Contains génère peu de garbage, mais on pourrait comparer des IDs ou Tags si possible.
        if (currentMorphObjectName != null && obj.name.Contains(currentMorphObjectName))
            return;

        string objName = obj.name;
        
        // Boucle standard au lieu de foreach si MimeObjects est grand (ici ça va)
        for(int i = 0; i < MimeObjects.Length; i++)
        {
            GameObject mimeObj = MimeObjects[i];
            if (objName.Contains(mimeObj.name))
            {
                MeshFilter targetMeshFilter = mimeObj.GetComponent<MeshFilter>();
                MeshRenderer targetRenderer = mimeObj.GetComponent<MeshRenderer>();

                if (targetMeshFilter != null && targetRenderer != null)
                {
                    MeshFilter playerMeshFilter = GetComponent<MeshFilter>();
                    MeshRenderer playerMeshRenderer = GetComponent<MeshRenderer>();
                    MeshCollider playerMeshCollider = GetComponent<MeshCollider>();

                    if (playerMeshFilter != null && playerMeshRenderer != null && playerMeshCollider != null)
                    {
                        currentMorphObjectName = mimeObj.name;
                        morphVFX.Play();
                        
                        playerMeshFilter.sharedMesh = targetMeshFilter.sharedMesh;
                        playerMeshRenderer.sharedMaterial = targetRenderer.sharedMaterial; // SharedMaterial économise mémoire
                        playerMeshCollider.sharedMesh = targetMeshFilter.sharedMesh;
                        
                        transform.localScale = mimeObj.transform.localScale;
                        currentCooldownTime = morphCooldownTime;

                        StartCoroutine(AnimateScale(mimeObj.transform.localScale));
                    }
                }
                break;
            }
        }
    }

    private void ResetPlayerMorph()
    {
        if (currentMorphObjectName == null)
            return;
            
        morphVFX.Play();
        GetComponent<MeshFilter>().mesh = basePlayerMesh;
        GetComponent<MeshRenderer>().material = basePlayerMaterial;
        GetComponent<MeshCollider>().sharedMesh = basePlayerMesh;
        
        transform.localScale = baseplayerScale;
        currentMorphObjectName = null;
        StartCoroutine(AnimateScale(baseplayerScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = Vector3.zero;
        float elapsed = 0f;

        if (morphVFX != null && morphVFX.transform.parent == transform)
        {
            morphVFX.transform.SetParent(null);
        }

        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimationDuration;
            float curveValue = scaleCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

            if (morphVFX != null)
            {
                morphVFX.transform.position = transform.position;
            }

            yield return null;
        }

        transform.localScale = targetScale;

        if (morphVFX != null)
        {
            morphVFX.transform.SetParent(transform);
            morphVFX.transform.localPosition = Vector3.zero;
            morphVFX.transform.localScale = Vector3.one;
        }
    }

    private void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        Vector3 forward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(playerTransform.position, mimeRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 leftBoundary = Quaternion.Euler(0, -maxAngle, 0) * forward2D;
        Vector3 rightBoundary = Quaternion.Euler(0, maxAngle, 0) * forward2D;

        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftBoundary * mimeRange);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightBoundary * mimeRange);

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

        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + forward2D * mimeRange);

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
        
        // Cache des composants initiaux
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();
        
        if(filter != null) basePlayerMesh = filter.sharedMesh; // SharedMesh est mieux pour la lecture
        if(renderer != null) basePlayerMaterial = renderer.sharedMaterial;
        
        baseplayerScale = transform.localScale;
        return this;
    }

    public override ISkills DeactivateSkill()
    {
        // Nettoyage si on désactive le skill alors qu'un objet est sélectionné
        if(currentSelectedObject != null)
        {
            HighlightObject(null);
        }

        base.DeactivateSkill();
        if (morphVFX != null) morphVFX.Stop();
        return this;
    }
}