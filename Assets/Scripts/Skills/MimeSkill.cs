using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Netcode;
using Unity.Collections;

public class MimeSkill : ASkills
{
    [Header("Mime Skill Settings")]
    [SerializeField] private Material MimeObjectMaterial;
    [SerializeField] private float mimeRange = 3f;
    private Transform playerTransform;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 60f;
    [SerializeField] private VisualEffect morphVFX;
    [SerializeField] private GameObject[] MimeObjects; 

    [Header("Scale Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private GameObject currentSelectedObject;
    private Renderer currentSelectedRenderer;
    private NetworkVariable<FixedString64Bytes> netMorphName = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private float morphCooldownTime = 2f;
    private float currentCooldownTime = 0f;

    private Mesh basePlayerMesh;
    private Material basePlayerMaterial;
    private Vector3 baseplayerScale;

    // Buffers
    private List<Material> _originalMaterialsBuffer = new List<Material>();
    private List<Material> _tempMaterialsBuffer = new List<Material>();
    private Collider[] _hitCollidersBuffer = new Collider[20]; 

    private void Start()
    {
        morphVFX = GetComponentInChildren<VisualEffect>();
        if (morphVFX != null) morphVFX.Stop();
    }

    public override void OnNetworkSpawn()
    {
        netMorphName.OnValueChanged += OnMorphStateChanged;
        
        if (!netMorphName.Value.IsEmpty)
        {
            ApplyMorphLocal(netMorphName.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        netMorphName.OnValueChanged -= OnMorphStateChanged;
    }

    private void Update()
    {
        if (currentCooldownTime > 0f)
            currentCooldownTime -= Time.deltaTime;

        if (IsOwner && isActive)
        {
            FindNearestMimeObject();
        }
    }

    public override void MainAction()
    {
        if (!IsOwner) return;
        RequestMorphServerRpc(currentSelectedObject != null ? currentSelectedObject.name : "");
    }

    public override void SecondaryAction()
    {
        if (!IsOwner) return;
        RequestMorphResetServerRpc();
    }

    [ServerRpc]
    private void RequestMorphServerRpc(string objectName)
    {
        if (currentCooldownTime > 0f) return;
        
        if (!string.IsNullOrEmpty(objectName))
        {
            netMorphName.Value = objectName;
            currentCooldownTime = morphCooldownTime;
        }
    }

    [ServerRpc]
    private void RequestMorphResetServerRpc()
    {
        netMorphName.Value = "";
    }

    private void OnMorphStateChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        string newName = current.ToString();
        if (string.IsNullOrEmpty(newName))
        {
            ResetPlayerMorphLocal();
        }
        else
        {
            ApplyMorphLocal(newName);
        }
    }

    private void FindNearestMimeObject()
    {
        if (playerTransform == null) return;
        int hitCount = Physics.OverlapSphereNonAlloc(playerTransform.position, mimeRange, _hitCollidersBuffer);

        GameObject nearestMimeObject = null;
        float minDistance = float.MaxValue;
        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _hitCollidersBuffer[i];
            if (!col.CompareTag("MimeObject")) continue;

            Vector3 objPos2D = new Vector3(col.transform.position.x, 0, col.transform.position.z);
            Vector3 toPoint = objPos2D - playerPos2D;
            float angleToObject = Vector3.Angle(playerForward2D, toPoint);

            if (angleToObject > maxAngle) continue;
            if (Vector3.Dot(toPoint, playerForward2D) < 0) continue;

            float dist = Vector3.Distance(objPos2D, playerPos2D + playerForward2D * Vector3.Dot(toPoint, playerForward2D)); // Approx dist perpendiculaire
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestMimeObject = col.gameObject;
            }
        }
        HighlightObject(nearestMimeObject);
    }

    private void HighlightObject(GameObject obj)
    {
        if (currentSelectedObject == obj) return;

        // Reset ancien
        if (currentSelectedObject != null && currentSelectedRenderer != null && _originalMaterialsBuffer.Count > 0)
        {
            currentSelectedRenderer.SetMaterials(_originalMaterialsBuffer);
        }

        currentSelectedObject = obj;
        _originalMaterialsBuffer.Clear();
        _tempMaterialsBuffer.Clear();

        // Apply nouveau
        if (currentSelectedObject != null)
        {
            currentSelectedRenderer = currentSelectedObject.GetComponent<Renderer>();
            if (currentSelectedRenderer != null)
            {
                currentSelectedRenderer.GetMaterials(_originalMaterialsBuffer);
                _tempMaterialsBuffer.AddRange(_originalMaterialsBuffer);
                _tempMaterialsBuffer.Add(MimeObjectMaterial);
                currentSelectedRenderer.SetMaterials(_tempMaterialsBuffer);
            }
        }
        else
        {
            currentSelectedRenderer = null;
        }
    }

    private void ApplyMorphLocal(string targetName)
    {
        for(int i = 0; i < MimeObjects.Length; i++)
        {
            GameObject mimeObj = MimeObjects[i];
            if (targetName.Contains(mimeObj.name))
            {
                MeshFilter targetMeshFilter = mimeObj.GetComponent<MeshFilter>();
                MeshRenderer targetRenderer = mimeObj.GetComponent<MeshRenderer>();

                if (targetMeshFilter != null && targetRenderer != null)
                {
                    MeshFilter playerFilter = GetComponent<MeshFilter>();
                    MeshRenderer playerRenderer = GetComponent<MeshRenderer>();
                    MeshCollider playerCollider = GetComponent<MeshCollider>();

                    if (playerFilter != null && playerRenderer != null)
                    {
                        if(morphVFX != null) morphVFX.Play();
                        
                        playerFilter.sharedMesh = targetMeshFilter.sharedMesh;
                        playerRenderer.sharedMaterial = targetRenderer.sharedMaterial;
                        if(playerCollider != null) playerCollider.sharedMesh = targetMeshFilter.sharedMesh;
                        
                        StartCoroutine(AnimateScale(mimeObj.transform.localScale));
                        currentCooldownTime = morphCooldownTime;
                    }
                }
                return;
            }
        }
    }

    private void ResetPlayerMorphLocal()
    {
        if(morphVFX != null) morphVFX.Play();
        
        GetComponent<MeshFilter>().mesh = basePlayerMesh;
        GetComponent<MeshRenderer>().material = basePlayerMaterial;
        GetComponent<MeshCollider>().sharedMesh = basePlayerMesh;
        
        StartCoroutine(AnimateScale(baseplayerScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        if (morphVFX != null && morphVFX.transform.parent == transform) morphVFX.transform.SetParent(null);

        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleCurve.Evaluate(t));
            
            if (morphVFX != null) morphVFX.transform.position = transform.position;
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
        
        var filter = GetComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();
        if(filter != null) basePlayerMesh = filter.sharedMesh;
        if(renderer != null) basePlayerMaterial = renderer.sharedMaterial;
        baseplayerScale = transform.localScale;

        if(!netMorphName.Value.IsEmpty) ApplyMorphLocal(netMorphName.Value.ToString());

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        if(IsOwner && currentSelectedObject != null) HighlightObject(null);
        base.DeactivateSkill();
        return this;
    }
}