using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LockpickGame : AHackingGame
{
    [Header("UI References")]
    [SerializeField] private GameObject gameContainer;
    [SerializeField] private RectTransform barBackground;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform cursor;

    [Header("Game Settings")]
    [SerializeField] private float baseSpeed = 200f;
    [SerializeField] private float baseZoneWidth = 100f;
    [SerializeField] private float minZoneWidth = 20f;

    private PlayerController playerController;
    private PlayerInput inputActions;
    
    private float currentSpeed;
    private float barWidth;
    private bool movingRight = true;
    private float leftLimit;
    private float rightLimit;

    private float zoneMinX;
    private float zoneMaxX;

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (inputActions == null)
            inputActions = GetComponentInParent<PlayerInput>();

        ResetVisuals();
    }

    protected override void OnGameStart()
    {
        gameContainer.SetActive(true);

        float difficultyMultiplier = currentDifficulty * 0.5f; 
        currentSpeed = baseSpeed + (baseSpeed * difficultyMultiplier * 0.5f);
        
        float currentZoneWidth = Mathf.Max(baseZoneWidth - (currentDifficulty * 15f), minZoneWidth);

        barWidth = barBackground.rect.width;
        leftLimit = -(barWidth / 2f);
        rightLimit = barWidth / 2f;

        successZone.sizeDelta = new Vector2(currentZoneWidth, successZone.sizeDelta.y);

        float maxOffset = (barWidth / 2f) - (currentZoneWidth / 2f) - 10f;
        float randomX = Random.Range(-maxOffset, maxOffset);
        successZone.anchoredPosition = new Vector2(randomX, 0);

        zoneMinX = randomX - (currentZoneWidth / 2f);
        zoneMaxX = randomX + (currentZoneWidth / 2f);

        cursor.anchoredPosition = new Vector2(leftLimit, 0);
        movingRight = true;

        if (inputActions != null)
        {
            InputAction actionCheck = inputActions.actions["MainAction"]; 
            InputAction leaveAction = inputActions.actions["Leave"];

            actionCheck.performed += CheckLock;
            leaveAction.performed += CancelGame;
        }

        if (playerController != null)
            playerController.enabled = false;
    }

    protected override void HandleInput()
    {
        MoveCursor();
    }

    private void MoveCursor()
    {
        float moveAmount = currentSpeed * Time.deltaTime;
        float currentX = cursor.anchoredPosition.x;

        if (movingRight)
        {
            currentX += moveAmount;
            if (currentX >= rightLimit)
            {
                currentX = rightLimit;
                movingRight = false;
            }
        }
        else
        {
            currentX -= moveAmount;
            if (currentX <= leftLimit)
            {
                currentX = leftLimit;
                movingRight = true;
            }
        }

        cursor.anchoredPosition = new Vector2(currentX, 0);
    }

    private void CheckLock(InputAction.CallbackContext ctx)
    {
        float cursorX = cursor.anchoredPosition.x;

        if (cursorX >= zoneMinX && cursorX <= zoneMaxX)
            WinGame();
        else
            FailGame();
    }

    private void CancelGame(InputAction.CallbackContext ctx)
    {
        FailGame();
    }

    protected override void ResetVisuals()
    {
        gameContainer.SetActive(false);

        if (inputActions != null)
        {
            InputAction actionCheck = inputActions.actions["MainAction"];
            InputAction leaveAction = inputActions.actions["Leave"];

            if (actionCheck != null)
                actionCheck.performed -= CheckLock;
            if (leaveAction != null)
                leaveAction.performed -= CancelGame;
        }

        if (playerController != null)
            playerController.enabled = true;
    }
}