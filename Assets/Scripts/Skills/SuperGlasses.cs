using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SuperGlasses : ASkills
{
    [SerializeField] private GameObject superGlassesObject;
    [SerializeField] private Light superGlassesLight;
    [SerializeField] private float glassesCooldown = 0.5f;
    [SerializeField] private Reveal tmpObject;
    [SerializeField] private AHackingGame[] hackingGameList;
    private float lastGlassesTime = -Mathf.Infinity;

    private bool isGlassesOn = false;

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
    }

    private void Update()
    {
        if (!isActive) return;

        if(lastGlassesTime > 0)
            lastGlassesTime -= Time.deltaTime;
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
        // Implementation for SuperGlasses secondary action
        Debug.Log("Is element revealed: " + tmpObject.GetIsIlluminated());
        if (tmpObject.GetIsIlluminated())
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

        selectedPrefab.Initialize(difficulty, 100f); // Exemple de timeLimit de 10 secondes
        selectedPrefab.BeginGame(
            onWin: () => {
                Debug.Log("Coffre ouvert !");
                // Donner le loot au joueur
            },
            onLose: () => {
                Debug.Log("Échec, le garde a entendu !");
                // Alerter les gardes
            }
        );
    }

    private void ToggleGlasses()
    {
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
