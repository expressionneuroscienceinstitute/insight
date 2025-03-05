using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Define the possible game states.
    public enum GameState
    {
        MainMenu,
        Options,
        InGame,
        Paused,
        Results
    }

    // Current state.
    public GameState currentState;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject inGameHUD;
    public GameObject pauseMenuPanel;
    public GameObject resultsPanel;

    void Awake()
    {
        // Implement the singleton pattern.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Persist between scenes.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start at the Paused state.
        ChangeState(GameState.InGame);
    }

    // Call this method to change game state.
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        UpdateUIForState();
    }

    // Update the UI panels based on the current game state.
    void UpdateUIForState()
    {
        // Turn off all panels initially.
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(false);
        if (inGameHUD) inGameHUD.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(false);

        // Enable the panel(s) that correspond to the current state.
        switch (currentState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                break;
            case GameState.Options:
                if (optionsPanel) optionsPanel.SetActive(true);
                break;
            case GameState.InGame:
                if (inGameHUD) inGameHUD.SetActive(true);
                break;
            case GameState.Paused:
                if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
                break;
            case GameState.Results:
                if (resultsPanel) resultsPanel.SetActive(true);
                break;
        }
    }

    // These methods can be hooked up to UI buttons (using the Inspector)
    // to change the game state.

    public void StartGame()
    {
        // Optionally load a game scene or initialize game parameters here.
        ChangeState(GameState.InGame);
    }

    public void OpenOptions()
    {
        ChangeState(GameState.Options);
    }

    public void ReturnToMainMenu()
    {
        // Optionally reload a scene if needed.
        ChangeState(GameState.MainMenu);
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;  // Pause game time.
    }

    public void ResumeGame()
    {
        ChangeState(GameState.InGame);
        Time.timeScale = 1f;  // Resume game time.
    }

    public void ShowResults()
    {
        ChangeState(GameState.Results);
    }

    // Optional: Implement a method to handle quitting the game.
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
