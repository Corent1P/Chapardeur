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

    // Position et rotation initiales des lunettes (définies dans le prefab)
    private Vector3 initialGlassesPosition;
    private Quaternion initialGlassesRotation; // Rotation d'origine = lunettes relevées (désactivées)
    
    // Offsets pour ajuster la position des lunettes quand elles sont actives (sur les yeux)
    [Header("Position Active Offsets")]
    [SerializeField] private Vector3 activePositionOffset = new Vector3(0f, -0.05f, -0.02f); // (x, y=hauteur, z=avancée)

    private NetworkVariable<bool> netIsGlassesOn = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        netIsGlassesOn.OnValueChanged += OnGlassesStateChanged;
        UpdateGlassesVisual(netIsGlassesOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        netIsGlassesOn.OnValueChanged -= OnGlassesStateChanged;
    }

    private void OnGlassesStateChanged(bool previous, bool current)
    {
        UpdateGlassesVisual(current);
    }

    // Cette fonction gère maintenant TOUT l'aspect local (Visuel + Logique Reveal)
    private void UpdateGlassesVisual(bool isOn)
    {
        StopAllCoroutines(); // Sécurité pour éviter conflit d'anim
        StartCoroutine(MoveGlasses(isOn)); // true = position active (sur les yeux), false = position inactive (relevées)
        
        isGlassesOn = isOn;

        if (superGlassesLight != null) 
        {
            superGlassesLight.enabled = isOn;

            // C'est ici que chaque client met à jour sa propre liste Reveal
            if (isOn)
                Reveal.RegisterLight(superGlassesLight);
            else
                Reveal.UnregisterLight(superGlassesLight);
        }
    }

    IEnumerator MoveGlasses(bool toActivePosition)
    {
        float time = 0f;
        float duration = 0.3f;
        Quaternion startRotation = superGlassesObject.transform.localRotation;
        Vector3 startPosition = superGlassesObject.transform.localPosition;
        
        // Position active = lunettes droites sur les yeux (rotation 0) + offset de position
        // Position inactive = rotation et position d'origine du prefab (lunettes relevées sur le front)
        Quaternion targetRotation = toActivePosition 
            ? Quaternion.identity 
            : initialGlassesRotation;
        Vector3 targetPosition = toActivePosition 
            ? initialGlassesPosition + activePositionOffset 
            : initialGlassesPosition;

        while (time < duration)
        {
            float t = time / duration;
            superGlassesObject.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            superGlassesObject.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            time += Time.deltaTime;
            yield return null;
        }
        superGlassesObject.transform.localRotation = targetRotation;
        superGlassesObject.transform.localPosition = targetPosition;
    }

    private void Update()
    {
        if (!IsOwner || !isActive) return;

        if (lastGlassesTime > 0) lastGlassesTime -= Time.deltaTime;

        if (isGlassesOn)
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
        // Le serveur change juste la variable.
        // Le callback OnValueChanged fera le travail visuel sur tous les clients.
        netIsGlassesOn.Value = !netIsGlassesOn.Value;
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
        // ... (Code identique à votre version) ...
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
            }
        );
    }

    [ServerRpc]
    private void UnlockObjectServerRpc(ulong targetObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject netObj))
        {
            // var revealScript = netObj.GetComponent<Reveal>();
            // revealScript.Unlock(); 
            Debug.Log($"Objet {targetObjectId} déverrouillé par le serveur !");
        }
    }

    private void Awake()
    {
        // Sauvegarder la position et rotation initiales des lunettes (celles du prefab)
        if (superGlassesObject != null)
        {
            initialGlassesPosition = superGlassesObject.transform.localPosition;
            initialGlassesRotation = superGlassesObject.transform.localRotation;
        }
    }

    private void Start()
    {
        if (superGlassesObject != null) superGlassesObject.SetActive(false);
        if (superGlassesLight != null) superGlassesLight.enabled = false;
        
        playerRigidbody = GetComponentInParent<Rigidbody>();
        playerTransform = transform;
    }

    public override ISkills ActivateSkill()
    {
        Debug.Log("SuperGlasses Activated");
        base.ActivateSkill();
        
        // Remettre les lunettes à leur position et rotation correcte avant de les afficher
        if (superGlassesObject != null)
        {
            // Activé = position avec offset + rotation droite, Désactivé = position et rotation d'origine
            superGlassesObject.transform.localPosition = netIsGlassesOn.Value 
                ? initialGlassesPosition + activePositionOffset 
                : initialGlassesPosition;
            superGlassesObject.transform.localRotation = netIsGlassesOn.Value 
                ? Quaternion.identity 
                : initialGlassesRotation;
        }
        
        superGlassesObject.SetActive(true);
        
        // On synchronise l'état visuel avec l'état réseau actuel
        UpdateGlassesVisual(netIsGlassesOn.Value);

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        superGlassesObject.SetActive(false);
        
        // On force l'extinction locale en quittant le skill
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = false;
            Reveal.UnregisterLight(superGlassesLight);
        }

        // Si on est le propriétaire et que les lunettes étaient allumées, on demande au serveur de les éteindre
        // pour que l'état reste cohérent si on reprend le skill plus tard
        if (IsOwner && netIsGlassesOn.Value)
        {
            ToggleGlassesServerRpc();
        }

        return this;
    }

    // ... (Reste des fonctions FindBestHiddenElement, OnDrawGizmos identiques) ...
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