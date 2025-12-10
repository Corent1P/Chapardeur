using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Events;

public enum GameMode
{
    Speedrun,
    Hardcore,
    Chill
}

public enum GameState
{
    Menu,
    Lobby,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private GameMode currentGameMode = GameMode.Chill;
    [SerializeField] private float speedrunTimeLimit = 180f;

    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnGameOver;
    public UnityEvent OnVictory;
    public UnityEvent OnLifeLost;
    public UnityEvent<float> OnTimerUpdated;

    // Network Variables
    private NetworkVariable<int> netLives = new NetworkVariable<int>(3);
    private NetworkVariable<bool> netGameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netObjectiveStolen = new NetworkVariable<bool>(false);
    private NetworkVariable<float> netGameTime = new NetworkVariable<float>(0f);
    private NetworkVariable<int> netStars = new NetworkVariable<int>(0);

    private GameState currentState = GameState.Menu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) InitializeGameMode();

        netLives.OnValueChanged += OnLivesChanged;
        netGameTime.OnValueChanged += OnGameTimeChanged;
        netObjectiveStolen.OnValueChanged += OnObjectiveStolenChanged;
    }

    public override void OnNetworkDespawn()
    {
        netLives.OnValueChanged -= OnLivesChanged;
        netGameTime.OnValueChanged -= OnGameTimeChanged;
        netObjectiveStolen.OnValueChanged -= OnObjectiveStolenChanged;
    }

    private void Update()
    {
        if (!IsServer || !netGameActive.Value) return;

        netGameTime.Value += Time.deltaTime;

        if (currentGameMode == GameMode.Speedrun && netGameTime.Value >= speedrunTimeLimit)
        {
            GameOver(false);
        }
    }

    private void InitializeGameMode()
    {
        switch (currentGameMode)
        {
            case GameMode.Hardcore:
                netLives.Value = 1;
                break;
            case GameMode.Chill:
                netLives.Value = 3;
                break;
            case GameMode.Speedrun:
                netLives.Value = 3;
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (netGameActive.Value) return;

        netGameActive.Value = true;
        netGameTime.Value = 0f;
        netObjectiveStolen.Value = false;
        currentState = GameState.Playing;

        OnGameStart?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerDetectedServerRpc()
    {
        if (!netGameActive.Value) return;

        netLives.Value--;

        if (netLives.Value <= 0)
        {
            GameOver(false);
        }
        else
        {
            OnLifeLost?.Invoke();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ObjectiveStolenServerRpc()
    {
        if (!netGameActive.Value || netObjectiveStolen.Value) return;

        netObjectiveStolen.Value = true;
        CalculateStars();
        Victory();
    }

    public void PauseGame()
    {
        if (!netGameActive.Value) return;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!netGameActive.Value) return;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
    }

    private void GameOver(bool success)
    {
        if (!IsServer) return;

        netGameActive.Value = false;
        currentState = success ? GameState.Victory : GameState.GameOver;

        if (success) OnVictory?.Invoke();
        else OnGameOver?.Invoke();
    }

    private void Victory() => GameOver(true);

    private void CalculateStars()
    {
        if (!IsServer) return;

        int stars = 0;
        if (netLives.Value == 3) stars++;
        if (netLives.Value >= 2) stars++;
        if (netGameTime.Value < speedrunTimeLimit * 0.5f) stars++;

        netStars.Value = Mathf.Clamp(stars, 0, 3);
    }

    private void OnLivesChanged(int previous, int current) => Debug.Log($"Vidas: {current}");
    private void OnGameTimeChanged(float previous, float current) => OnTimerUpdated?.Invoke(current);
    private void OnObjectiveStolenChanged(bool previous, bool current) => Debug.Log("¡Objetivo robado!");

    public int GetLives() => netLives.Value;
    public float GetGameTime() => netGameTime.Value;
    public bool IsGameActive() => netGameActive.Value;
    public int GetStars() => netStars.Value;
    public GameMode GetCurrentGameMode() => currentGameMode;
}