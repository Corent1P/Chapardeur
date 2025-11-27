using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

public class SuperGlasses : ASkills
{
    [SerializeField] private GameObject superGlassesObject;
    [SerializeField] private Light superGlassesLight;
    [SerializeField] private float glassesCooldown = 0.5f;
    [SerializeField] private AHackingGame[] hackingGameList;
    private float lastGlassesTime = -Mathf.Infinity;

    [SerializeField] [Range(0f, 30f)] private float selectionRange = 10f;
    [SerializeField][Range(0f, 180f)] private float maxAngle = 20f;
    private Transform playerTransform;
    private Reveal currentSelectedElement = null;


    private bool isGlassesOn = false;
    private bool isHackingGameActive = false;
    private Rigidbody playerRigidbody;

    private NetworkVariable<bool> netIsGlassesOn = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // On s'abonne pour voir les autres mettre leurs lunettes
        netIsGlassesOn.OnValueChanged += OnGlassesStateChanged;
        
        // Initialisation visuelle
        UpdateGlassesVisual(netIsGlassesOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        netIsGlassesOn.OnValueChanged -= OnGlassesStateChanged;
    }

    // Callback appelé sur TOUS les clients quand la variable change
    private void OnGlassesStateChanged(bool previous, bool current)
    {
        UpdateGlassesVisual(current);
    }

    private void UpdateGlassesVisual(bool isOn)
    {
        // Lance l'animation locale
        StartCoroutine(MoveGlasses(isOn ? 0f : -90f));
        if (superGlassesLight != null) superGlassesLight.enabled = isOn;
        isGlassesOn = isOn; // Met à jour la variable locale pour la logique
    }

    IEnumerator MoveGlasses(float degree)
    {
        float time = 0f;
        float duration = 0.3f;
        Quaternion initialRotation = superGlassesObject.transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(degree, 0f, 0f);

        while (time < duration)
        {
            superGlassesObject.transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        superGlassesObject.transform.localRotation = targetRotation;
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = isGlassesOn;
        }
    }

    private void Update()
    {
        // Seul le propriétaire fait les Raycast de détection
        if (!IsOwner || !isActive) return;

        if (lastGlassesTime > 0) lastGlassesTime -= Time.deltaTime;

        if (isGlassesOn) // isGlassesOn est sync via le callback
        {
            FindBestHiddenElement();
        }
    }

    public override void MainAction()
    {
        if (!IsOwner) return;
        if (lastGlassesTime > 0) return;
        if (isHackingGameActive) return;
        lastGlassesTime = glassesCooldown;

        ToggleGlassesServerRpc();
    }

    [ServerRpc]
    private void ToggleGlassesServerRpc()
    {
        netIsGlassesOn.Value = !netIsGlassesOn.Value;

        if (superGlassesLight != null)
        {
             if (isGlassesOn) 
                 Reveal.RegisterLight(superGlassesLight);
             else 
                 Reveal.UnregisterLight(superGlassesLight);
        }
    }

    private void UpdateLightRegistration()
    {
        if (superGlassesLight != null)
        {
            if (isGlassesOn && superGlassesLight.enabled)
                Reveal.RegisterLight(superGlassesLight);
            else
                Reveal.UnregisterLight(superGlassesLight);
        }
    }

    public override void SecondaryAction()
    {
        if (!IsOwner) return;

        if (!isGlassesOn || currentSelectedElement == null) return;

        if (currentSelectedElement.GetIsIlluminated())
        {
            StartLockpicking(2); 
        }
    }

    public void StartLockpicking(int difficulty)
    {
        AHackingGame selectedPrefab = hackingGameList[Random.Range(0, hackingGameList.Length)];
        isHackingGameActive = true;
        isSkillLocked = true;

        if(playerRigidbody != null) playerRigidbody.linearVelocity = Vector3.zero;

        selectedPrefab.Initialize(difficulty, 100f);
        selectedPrefab.BeginGame(
            onWin: () => {
                isHackingGameActive = false;
                isSkillLocked = false;

                if(currentSelectedElement != null)
                {
                    var netObj = currentSelectedElement.GetComponent<NetworkObject>();
                    if(netObj != null)
                    {
                        UnlockObjectServerRpc(netObj.NetworkObjectId);
                    }
                }
            },
            onLose: () => {
                isHackingGameActive = false;
                isSkillLocked = false;
                // Alerter les gardes (ServerRpc eventuel)
            }
        );
    }

    [ServerRpc]
    private void UnlockObjectServerRpc(ulong targetObjectId)
    {
        // Le serveur valide l'ouverture
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject netObj))
        {
            // Logique d'ouverture (ex: appeler une fonction sur le coffre)
            var revealScript = netObj.GetComponent<Reveal>();
            // revealScript.Unlock(); 
            Debug.Log($"Objet {targetObjectId} déverrouillé par le serveur !");
        }
    }

    private void Start()
    {
        if (superGlassesObject != null)
        {
            superGlassesObject.SetActive(false);
        }
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = false;
        }
        playerRigidbody = GetComponentInParent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogWarning("Rigidbody component not found on PlayerController.");
        }
        playerTransform = transform;
    }

    public override ISkills ActivateSkill()
    {
        Debug.Log("SuperGlasses Activated");
        base.ActivateSkill();
        superGlassesObject.SetActive(true);
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = isGlassesOn;
        }

        UpdateLightRegistration();

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        superGlassesObject.SetActive(false);
        superGlassesLight.enabled = false;
        Reveal.UnregisterLight(superGlassesLight);

        if (isGlassesOn)
            ToggleGlassesServerRpc();

        return this;
    }

    private void FindBestHiddenElement()
    {
        if (playerTransform == null) return;

        GameObject[] hidingElements = GameObject.FindGameObjectsWithTag("Hiding Element");
        
        GameObject bestElement = null;
        float minDistance = float.MaxValue;

        Vector3 playerPos2D = new Vector3(playerTransform.position.x, 0, playerTransform.position.z);
        Vector3 playerForward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;

        foreach (GameObject element in hidingElements)
        {
            Vector3 elementPos2D = new Vector3(element.transform.position.x, 0, element.transform.position.z);
            
            float distanceToPlayer = Vector3.Distance(playerPos2D, elementPos2D);
            if (distanceToPlayer > selectionRange)
                continue;
            Vector3 toElement = elementPos2D - playerPos2D;
            float angleToElement = Vector3.Angle(playerForward2D, toElement);
            
            if (angleToElement > maxAngle)
                continue;
            
            float projectionLength = Vector3.Dot(toElement, playerForward2D);
            
            if (projectionLength < 0)
                continue;

            Vector3 closestElementOnLine = playerPos2D + playerForward2D * projectionLength;
            
            float perpendicularDistance = Vector3.Distance(elementPos2D, closestElementOnLine);

            if (perpendicularDistance < minDistance)
            {
                minDistance = perpendicularDistance;
                bestElement = element;
            }
        }

        UpdateSelectedElement(bestElement);
    }

    private void UpdateSelectedElement(GameObject element)
    {
        if (element != null)
        {
            currentSelectedElement = element.GetComponent<Reveal>();
        }
        else
        {
            currentSelectedElement = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        Vector3 forward2D = new Vector3(playerTransform.forward.x, 0, playerTransform.forward.z).normalized;
        
        // Dessiner la portée du grappin (sphère complète)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(playerTransform.position, selectionRange);

        // Dessiner le cône de vision (angle de vue)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 leftBoundary = Quaternion.Euler(0, -maxAngle, 0) * forward2D;
        Vector3 rightBoundary = Quaternion.Euler(0, maxAngle, 0) * forward2D;
        
        // Lignes des limites du cône
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + leftBoundary * selectionRange);
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + rightBoundary * selectionRange);
        
        // Arc pour visualiser le cône (approximation avec plusieurs lignes)
        int arcSegments = 20;
        Vector3 previousPoint = playerTransform.position + leftBoundary * selectionRange;
        for (int i = 1; i <= arcSegments; i++)
        {
            float angle = Mathf.Lerp(-maxAngle, maxAngle, i / (float)arcSegments);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward2D;
            Vector3 point = playerTransform.position + direction * selectionRange;
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Dessiner la ligne de visée centrale
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + forward2D * selectionRange);

        // Dessiner le point sélectionné
        if (currentSelectedElement != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerTransform.position, currentSelectedElement.transform.position);
            Gizmos.DrawWireSphere(currentSelectedElement.transform.position, 0.5f);
        }
    }
}
