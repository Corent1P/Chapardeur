using UnityEngine;
using System;

public abstract class AHackingGame : MonoBehaviour, IHackingGame
{
    protected int currentDifficulty;
    protected Action OnWinCallback;
    protected Action OnLoseCallback;

    protected bool isGameActive = false;
    private float timeLimit;
    private float timer;

    // 1. Implémentation de l'interface
    public void Initialize(int difficulty, float timeLimit)
    {
        currentDifficulty = difficulty;
        this.timeLimit = timeLimit;
        timer = timeLimit;
        // Reset visuel (méthode abstraite que les enfants définiront)
        ResetVisuals(); 
    }

    public void BeginGame(Action onWin, Action onLose)
    {
        OnWinCallback = onWin;
        OnLoseCallback = onLose;
        isGameActive = true;
        
        // Logique spécifique au démarrage du mini-jeu enfant
        OnGameStart(); 
    }

    public void ForceStop()
    {
        isGameActive = false;
        gameObject.SetActive(false);
    }

    // 2. La boucle principale (Update)
    protected virtual void Update()
    {
        if (!isGameActive) return;

        // Gestion commune du Timer
        if (timeLimit > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                FailGame();
            }
        }

        // Délègue la logique de gameplay à l'enfant
        HandleInput();
    }

    // 3. Méthodes pour finir le jeu (utilisées par les enfants)
    protected void WinGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Serrure crochetée !");
        OnWinCallback?.Invoke();
        // Optionnel : Animation de victoire ici

        ResetVisuals();
    }

    protected void FailGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Crochet cassé !");
        OnLoseCallback?.Invoke();
        // Optionnel : Animation de défaite ici

        ResetVisuals();
    }

    // 4. Méthodes Abstraites (Les enfants DOIVENT les implémenter)
    protected abstract void OnGameStart(); // Ex: Lancer une aiguille qui tourne
    protected abstract void HandleInput(); // Ex: Appuyer sur Espace au bon moment
    protected abstract void ResetVisuals(); // Remettre l'UI à zéro
}