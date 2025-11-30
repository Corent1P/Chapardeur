using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AlignRadarGame : AHackingGame
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject[] radarScreen;
    [SerializeField] private GameObject alignmentIndicators;
    [SerializeField] private float[] alignementsToReach;
    [SerializeField] private float alignmentSpeed = 100f;

    private float currentAlignment = 0f;
    private Tuple<GameObject, float>[] alignementList;
    private int currentAlignementIndex = 0;
    private PlayerController playerController;
    private PlayerInput inputActions;
    private Vector2 moveInput;


    private void Awake()
    {
        background.SetActive(false);
        alignmentIndicators.SetActive(false);
        foreach (var screen in radarScreen)
        {
            screen.SetActive(false);
        }
    }

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found in scene.");
        }
        if (inputActions == null)
            inputActions = GetComponentInParent<PlayerInput>();
        alignementList = new Tuple<GameObject, float>[radarScreen.Length];
        for (int i = 0; i < radarScreen.Length; i++)
        {
            alignementList[i] = new Tuple<GameObject, float>(radarScreen[i], alignementsToReach[i]);
        }
    }

    protected override void HandleInput()
    {
        Debug.Log("AlignRadarGame handling input...");
        RotateIndicator();
    }

    private void RotateIndicator()
    {
        currentAlignment += -moveInput.y * alignmentSpeed * Time.deltaTime;
        if (currentAlignment < -80f || currentAlignment > 80f)
            return;
        alignmentIndicators.transform.localRotation = Quaternion.Euler(0, 0, currentAlignment);

        if (Mathf.Abs(currentAlignment - alignementList[currentAlignementIndex].Item2) < 0.5f)
        {
            Debug.Log("Alignement réussi !");
            PlayNextAlignement();
        }
    }

    private void PlayNextAlignement()
    {
        if (currentAlignementIndex >= alignementList.Length - 1)
        {
            WinGame();
            return;
        }
        currentAlignment = 0f;
        alignmentIndicators.transform.localRotation = Quaternion.Euler(0, 0, currentAlignment);
        alignementList[currentAlignementIndex].Item1.SetActive(false);
        currentAlignementIndex++;
        alignementList[currentAlignementIndex].Item1.SetActive(true);
    }

    protected override void OnGameStart()
    {
        Debug.Log("AlignRadarGame started...");
        background.SetActive(true);
        currentAlignementIndex = 0;
        alignementList[currentAlignementIndex].Item1.SetActive(true);
        alignmentIndicators.SetActive(true);
        currentAlignment = 0f;
        alignmentIndicators.transform.localRotation = Quaternion.Euler(0, 0, currentAlignment);

        // inputActions.Enable();
        InputAction leaveAction = inputActions.actions["Leave"];
        InputAction moveAction = inputActions.actions["Move"];

        leaveAction.performed += ctx => FailGame();
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        playerController.enabled = false;
    }

    protected override void ResetVisuals()
    {
        Debug.Log("AlignRadarGame resetting visuals...");
        background.SetActive(false);
        alignementList[currentAlignementIndex].Item1.SetActive(false);
        alignmentIndicators.SetActive(false);

        // inputActions.Disable();
        InputAction leaveAction = inputActions.actions["Leave"];
        InputAction moveAction = inputActions.actions["Move"];

        leaveAction.performed -= ctx => FailGame();
        moveAction.performed -= ctx => moveInput = Vector2.zero;
        moveAction.canceled -= ctx => moveInput = Vector2.zero;

        playerController.enabled = true;
    }
}