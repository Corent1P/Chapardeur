using UnityEngine;

public class AlignRadarGame : AHackingGame
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject radarScreen;
    [SerializeField] private GameObject alignmentIndicator;
    [SerializeField] private float alignmentSpeed = 10f;

    private float currentAlignment = 0f;
    [SerializeField] private float alignmentToReach = 45f;
    private PlayerController playerController;

    private void Awake()
    {
        background.SetActive(false);
        radarScreen.SetActive(false);
        alignmentIndicator.SetActive(false);
    }

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found in scene.");
        }
    }

    protected override void HandleInput()
    {
        Debug.Log("AlignRadarGame handling input...");
    }

    protected override void OnGameStart()
    {
        Debug.Log("AlignRadarGame started...");
        background.SetActive(true);
        radarScreen.SetActive(true);
        alignmentIndicator.SetActive(true);

        playerController.enabled = false;
    }

    protected override void ResetVisuals()
    {
        Debug.Log("AlignRadarGame resetting visuals...");
        background.SetActive(false);
        radarScreen.SetActive(false);
        alignmentIndicator.SetActive(false);

        playerController.enabled = true;
    }
}