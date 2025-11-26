using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

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
    // [SerializeField] private Reveal tmpObject;
    private Reveal currentSelectedElement = null;


    private bool isGlassesOn = false;
    private bool isHackingGameActive = false;
    private Rigidbody playerRigidbody;


    // ---------------------------------------------------
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
    // ---------------------------------------------------

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

    private void Update()
    {
        if (!isActive) return;

        if(lastGlassesTime > 0)
            lastGlassesTime -= Time.deltaTime;

        if (isGlassesOn)
        {
            FindBestHiddenElement();
        }
    }

    public override void MainAction()
    {
        if (lastGlassesTime > 0) return;
        lastGlassesTime = glassesCooldown;
        // Implementation for SuperGlasses main action
        ToggleGlasses();
    }

    public override void SecondaryAction()
    {
        if (isGlassesOn == false) return;
        if (currentSelectedElement == null) return;
        // Implementation for SuperGlasses secondary action
        Debug.Log("Is element revealed: " + currentSelectedElement.GetIsIlluminated());
        if (currentSelectedElement.GetIsIlluminated())
        {
            Debug.Log("Element is not revealed, starting lockpicking mini-game.");
            StartLockpicking(2); // Exemple de difficulté
        }
        else
        {
            Debug.Log("Element is already revealed, no need to hack.");
        }
    }

    public void StartLockpicking(int difficulty)
    {
        AHackingGame selectedPrefab = hackingGameList[Random.Range(0, hackingGameList.Length)];

        isHackingGameActive = true;
        isSkillLocked = true;
        playerRigidbody.linearVelocity = Vector3.zero;
        selectedPrefab.Initialize(difficulty, 100f); // Exemple de timeLimit de 10 secondes
        selectedPrefab.BeginGame(
            onWin: () => {
                Debug.Log("Coffre ouvert !");
                isHackingGameActive = false;
                isSkillLocked = false;
                // Donner le loot au joueur
            },
            onLose: () => {
                Debug.Log("Échec, le garde a entendu !");
                isHackingGameActive = false;
                isSkillLocked = false;
                // Alerter les gardes
            }
        );
    }

    private void ToggleGlasses()
    {
        if (isHackingGameActive) return;
        isGlassesOn = !isGlassesOn;
        StartCoroutine(MoveGlasses(isGlassesOn ? 0f : -90f));
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

    public override ISkills ActivateSkill()
    {
        Debug.Log("SuperGlasses Activated");
        base.ActivateSkill();
        superGlassesObject.SetActive(true);
        if (superGlassesLight != null)
        {
            superGlassesLight.enabled = isGlassesOn;
        }

        return this;
    }

    public override ISkills DeactivateSkill()
    {
        base.DeactivateSkill();
        superGlassesObject.SetActive(false);
        superGlassesLight.enabled = false;
        if (isGlassesOn)
            ToggleGlasses();

        return this;
    }
}
