using UnityEngine;

public class TemplateHackingGame : AHackingGame
{
    [SerializeField] private Transform rotatingCursor;
    [SerializeField] private RectTransform targetZone;
    
    private float currentAngle = 0f;
    private float rotationSpeed;

    private float precision = 15f; // Exemple de précision, pourrait être lié à la difficulté

    protected override void ResetVisuals()
    {
        currentAngle = 0;
        rotatingCursor.localRotation = Quaternion.identity;
        // Ajuster la taille de la zone cible selon la difficulté
        targetZone.sizeDelta = new Vector2(precision * 50, 100);
    }

    protected override void OnGameStart()
    {
        // Plus c'est complexe, plus ça tourne vite
        rotationSpeed = 100f + (currentDifficulty * 20f);
    }

    protected override void HandleInput()
    {
        // Faire tourner le curseur
        currentAngle += rotationSpeed * Time.deltaTime;
        rotatingCursor.localRotation = Quaternion.Euler(0, 0, currentAngle);

        // Input du joueur
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckUnlock();
        }
    }

    private void CheckUnlock()
    {
        // Logique simple : est-ce que l'angle est dans la zone ?
        // Note : C'est une simplification, tu devras calculer l'angle correct par rapport à la zone cible
        float targetAngle = 0f; // Disons que la zone est en haut (0°)
        float tolerance = precision;

        if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < tolerance)
        {
            WinGame();
        }
        else
        {
            FailGame(); // Ou réduire la santé du crochet
        }
    }
}