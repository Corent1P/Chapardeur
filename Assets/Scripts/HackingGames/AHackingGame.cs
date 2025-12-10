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

    public void Initialize(int difficulty, float timeLimit)
    {
        currentDifficulty = difficulty;
        this.timeLimit = timeLimit;
        timer = timeLimit;

        ResetVisuals(); 
    }

    public void BeginGame(Action onWin, Action onLose)
    {
        OnWinCallback = onWin;
        OnLoseCallback = onLose;
        isGameActive = true;
        
        OnGameStart(); 
    }

    public void ForceStop()
    {
        isGameActive = false;
        gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (!isGameActive) return;

        if (timeLimit > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                FailGame();
            }
        }

        HandleInput();
    }

    protected void WinGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Serrure crochetée !");
        OnWinCallback?.Invoke();

        ResetVisuals();
    }

    protected void FailGame()
    {
        if (!isGameActive) return;
        isGameActive = false;
        Debug.Log("Crochet cassé !");
        OnLoseCallback?.Invoke();

        ResetVisuals();
    }

    protected abstract void OnGameStart();
    protected abstract void HandleInput();
    protected abstract void ResetVisuals();
}