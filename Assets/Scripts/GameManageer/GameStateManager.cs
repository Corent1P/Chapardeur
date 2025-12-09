using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Events;

public enum GameMode
{
    Speedrun,    // Completar en menos de X segundos
    Hardcore,    // 1 vida
    Chill        // 3 vidas, sin tiempo
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
    [SerializeField] private float speedrunTimeLimit = 180f; // 3 minutos para speedrun

    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnGameOver;
    public UnityEvent OnVictory;
    public UnityEvent OnLifeLost;
    public UnityEvent<float> OnTimerUpdated; // Para UI

    // Network Variables (sincronizadas)
    private NetworkVariable<int> netLives = new NetworkVariable<int>(3);
    private NetworkVariable<bool> netGameActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> netObjectiveStolen = new NetworkVariable<bool>(false);
    private NetworkVariable<float> netGameTime = new NetworkVariable<float>(0f);
    private NetworkVariable<int> netStars = new NetworkVariable<int>(0);

    // Local variables
    private GameState currentState = GameState.Menu;
    private float localTimer = 0f;
    private bool isTimerRunning = false;

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
        if (IsServer)
        {
            InitializeGameMode();
        }

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

        // Actualizar timer en el servidor
        netGameTime.Value += Time.deltaTime;

        // Verificar límite de tiempo para speedrun
        if (currentGameMode == GameMode.Speedrun && netGameTime.Value >= speedrunTimeLimit)
        {
            GameOver(false); // Perder por tiempo
        }
    }

    #region Game Mode Initialization
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
                // El timer ya se maneja en Update
                break;
        }
    }

    public void SetGameMode(GameMode mode)
    {
        if (!IsServer) return;
        currentGameMode = mode;
        InitializeGameMode();
    }
    #endregion

    #region Public Methods
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
            // Aquí podrías activar la alarma por X segundos
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
    #endregion

    #region Private Methods
    private void GameOver(bool success)
    {
        if (!IsServer) return;

        netGameActive.Value = false;
        currentState = success ? GameState.Victory : GameState.GameOver;

        if (success)
        {
            CalculateStars();
            OnVictory?.Invoke();
        }
        else
        {
            OnGameOver?.Invoke();
        }
    }

    private void Victory()
    {
        GameOver(true);
    }

    private void CalculateStars()
    {
        if (!IsServer) return;

        int stars = 0;
        float time = netGameTime.Value;

        // Sistema de 3 estrellas como Angry Birds
        if (netLives.Value == 3) stars++;
        if (netLives.Value >= 2) stars++;
        if (time < speedrunTimeLimit * 0.5f) stars++; // Menos de la mitad del tiempo

        netStars.Value = Mathf.Clamp(stars, 0, 3);
    }
    #endregion

    #region Network Callbacks
    private void OnLivesChanged(int previous, int current)
    {
        // Actualizar UI local
        Debug.Log($"Vidas cambiadas: {current}");
    }

    private void OnGameTimeChanged(float previous, float current)
    {
        // Actualizar timer en UI
        OnTimerUpdated?.Invoke(current);
    }

    private void OnObjectiveStolenChanged(bool previous, bool current)
    {
        if (current)
        {
            Debug.Log("¡Objetivo robado!");
        }
    }
    #endregion

    #region Getters
    public int GetLives() => netLives.Value;
    public float GetGameTime() => netGameTime.Value;
    public bool IsGameActive() => netGameActive.Value;
    public bool IsObjectiveStolen() => netObjectiveStolen.Value;
    public int GetStars() => netStars.Value;
    public GameMode GetCurrentGameMode() => currentGameMode;
    public GameState GetCurrentState() => currentState;
    #endregion
}