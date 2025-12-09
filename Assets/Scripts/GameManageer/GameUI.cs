using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Top Right Panel")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private Image[] starImages; // 3 estrellas

    [Header("Result Screen")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitle;
    [SerializeField] private TextMeshProUGUI resultTime;
    [SerializeField] private TextMeshProUGUI resultLives;
    [SerializeField] private Image[] resultStars;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    private GameStateManager gameManager;

    private void Start()
    {
        gameManager = GameStateManager.Instance;

        if (gameManager != null)
        {
            gameManager.OnGameStart.AddListener(OnGameStart);
            gameManager.OnGameOver.AddListener(OnGameOver);
            gameManager.OnVictory.AddListener(OnVictory);
            gameManager.OnLifeLost.AddListener(UpdateLivesUI);
            gameManager.OnTimerUpdated.AddListener(UpdateTimerUI);
        }

        playAgainButton.onClick.AddListener(OnPlayAgain);
        mainMenuButton.onClick.AddListener(OnMainMenu);

        resultPanel.SetActive(false);
        gamePanel.SetActive(false);
    }

    private void OnGameStart()
    {
        gamePanel.SetActive(true);
        resultPanel.SetActive(false);

        UpdateLivesUI();
        UpdateTimerUI(0f);
        UpdateModeUI();
    }

    private void UpdateLivesUI()
    {
        if (gameManager != null)
        {
            livesText.text = $"Lives: {gameManager.GetLives()}";
        }
    }

    private void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateModeUI()
    {
        if (gameManager != null)
        {
            modeText.text = $"Mode: {gameManager.GetCurrentGameMode()}";
        }
    }

    private void UpdateStars(int stars)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].enabled = i < stars;
        }
    }

    private void OnGameOver()
    {
        ShowResultScreen("GAME OVER", false);
    }

    private void OnVictory()
    {
        ShowResultScreen("VICTORY!", true);
    }

    private void ShowResultScreen(string title, bool success)
    {
        gamePanel.SetActive(false);
        resultPanel.SetActive(true);

        resultTitle.text = title;
        resultTitle.color = success ? Color.yellow : Color.red;

        if (gameManager != null)
        {
            float time = gameManager.GetGameTime();
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            resultTime.text = $"Time: {minutes:00}:{seconds:00}";

            resultLives.text = $"Lives: {gameManager.GetLives()}";

            int stars = gameManager.GetStars();
            for (int i = 0; i < resultStars.Length; i++)
            {
                resultStars[i].enabled = i < stars;
            }
        }
    }

    private void OnPlayAgain()
    {
        // Aquí necesitarás reiniciar la escena o el juego
        // Depende de cómo manejes las escenas
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        // Volver al menú principal
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStart.RemoveListener(OnGameStart);
            gameManager.OnGameOver.RemoveListener(OnGameOver);
            gameManager.OnVictory.RemoveListener(OnVictory);
            gameManager.OnLifeLost.RemoveListener(UpdateLivesUI);
            gameManager.OnTimerUpdated.RemoveListener(UpdateTimerUI);
        }
    }
}