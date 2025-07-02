using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Define the possible game states for VR diagnostic app
    public enum GameState
    {
        MainMenu,
        CalibrationSetup,
        CalibrationRunning,
        DiagnosticSetup,
        DiagnosticRunning,
        Results,
        Settings,
        Paused
    }

    // Current state
    public GameState currentState;

    [Header("Calibration Status")]
    public bool IsCalibrated { get; private set; }
    public bool CalibrationDataValid { get; private set; }
    
    [Header("Scene References (Addressables)")]
    [SerializeField] private AssetReference mainMenuScene;
    [SerializeField] private AssetReference calibrationScene;
    [SerializeField] private AssetReference diagnosticScene;
    [SerializeField] private AssetReference resultsScene;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject calibrationSetupPanel;
    public GameObject diagnosticSetupPanel;
    public GameObject resultsPanel;
    public GameObject settingsPanel;
    public GameObject pauseMenuPanel;

    // Events for UI updates
    public System.Action<GameState> OnStateChanged;
    public System.Action<bool> OnCalibrationStatusChanged;

    // Scene loading
    private AsyncOperationHandle<SceneInstance> currentSceneHandle;
    private bool isLoadingScene = false;

    void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to calibration events
            if (EyeCalibrationManager.Instance != null)
            {
                EyeCalibrationManager.Instance.OnCalibrated += OnCalibrationCompleted;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start at the MainMenu state
        ChangeState(GameState.MainMenu);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (EyeCalibrationManager.Instance != null)
        {
            EyeCalibrationManager.Instance.OnCalibrated -= OnCalibrationCompleted;
        }
        
        // Release any loaded scenes
        if (currentSceneHandle.IsValid())
        {
            Addressables.ReleaseInstance(currentSceneHandle);
        }
    }

    // Call this method to change game state
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        UpdateUIForState();
        OnStateChanged?.Invoke(currentState);
    }

    // Update the UI panels based on the current game state
    void UpdateUIForState()
    {
        // Turn off all panels initially
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (calibrationSetupPanel) calibrationSetupPanel.SetActive(false);
        if (diagnosticSetupPanel) diagnosticSetupPanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);

        // Enable the panel(s) that correspond to the current state
        switch (currentState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel) mainMenuPanel.SetActive(true);
                break;
            case GameState.CalibrationSetup:
                if (calibrationSetupPanel) calibrationSetupPanel.SetActive(true);
                break;
            case GameState.DiagnosticSetup:
                if (diagnosticSetupPanel) diagnosticSetupPanel.SetActive(true);
                break;
            case GameState.Results:
                if (resultsPanel) resultsPanel.SetActive(true);
                break;
            case GameState.Settings:
                if (settingsPanel) settingsPanel.SetActive(true);
                break;
            case GameState.Paused:
                if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
                break;
        }
    }

    // Scene transition methods using Addressables
    public void LoadCalibrationScene()
    {
        if (isLoadingScene) return;
        ChangeState(GameState.CalibrationSetup);
        StartCoroutine(LoadSceneAsync(calibrationScene, "CalibrationAlignment"));
    }

    public void LoadDiagnosticScene()
    {
        if (isLoadingScene) return;
        
        if (!IsCalibrated)
        {
            Debug.LogWarning("Cannot start diagnostic without calibration. Redirecting to calibration.");
            LoadCalibrationScene();
            return;
        }
        
        ChangeState(GameState.DiagnosticSetup);
        StartCoroutine(LoadSceneAsync(diagnosticScene, "DeveloperEnviroment"));
    }

    public void LoadResultsScene()
    {
        if (isLoadingScene) return;
        ChangeState(GameState.Results);
        StartCoroutine(LoadSceneAsync(resultsScene, "RoomEnviroment"));
    }

    public void ReturnToMainMenu()
    {
        if (isLoadingScene) return;
        ChangeState(GameState.MainMenu);
        StartCoroutine(LoadSceneAsync(mainMenuScene, "MainMenu"));
    }

    private IEnumerator LoadSceneAsync(AssetReference sceneReference, string fallbackSceneName)
    {
        isLoadingScene = true;

        // Use VRSceneTransitionManager if available
        if (VRSceneTransitionManager.Instance != null)
        {
            VRSceneTransitionManager.Instance.LoadSceneAsync(sceneReference, fallbackSceneName, () => {
                isLoadingScene = false;
            });
        }
        else
        {
            // Fallback to direct loading if VRSceneTransitionManager not available
            yield return LoadSceneDirectly(sceneReference, fallbackSceneName);
            isLoadingScene = false;
        }
    }

    private IEnumerator LoadSceneDirectly(AssetReference sceneReference, string fallbackSceneName)
    {
        // Release previous scene if loaded via Addressables
        if (currentSceneHandle.IsValid())
        {
            Addressables.ReleaseInstance(currentSceneHandle);
        }

        if (sceneReference != null && sceneReference.RuntimeKeyIsValid())
        {
            // Load scene using Addressables
            currentSceneHandle = Addressables.LoadSceneAsync(sceneReference, LoadSceneMode.Single);
            yield return currentSceneHandle;

            if (currentSceneHandle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Failed to load scene via Addressables. Falling back to SceneManager: {fallbackSceneName}");
                SceneManager.LoadScene(fallbackSceneName, LoadSceneMode.Single);
            }
        }
        else
        {
            // Fallback to traditional scene loading
            Debug.LogWarning($"Addressable reference not set. Using SceneManager to load: {fallbackSceneName}");
            SceneManager.LoadScene(fallbackSceneName, LoadSceneMode.Single);
        }
    }

    // Calibration management
    public void StartCalibration()
    {
        ChangeState(GameState.CalibrationRunning);
        
        if (EyeCalibrationManager.Instance != null)
        {
            EyeCalibrationManager.Instance.BeginCalibration();
        }
        else
        {
            Debug.LogError("EyeCalibrationManager not found!");
        }
    }

    private void OnCalibrationCompleted()
    {
        IsCalibrated = true;
        CalibrationDataValid = true;
        OnCalibrationStatusChanged?.Invoke(IsCalibrated);
        
        Debug.Log("Calibration completed successfully!");
        ChangeState(GameState.MainMenu);
    }

    public void ResetCalibration()
    {
        IsCalibrated = false;
        CalibrationDataValid = false;
        OnCalibrationStatusChanged?.Invoke(IsCalibrated);
    }

    // UI Button Methods
    public void StartDiagnostic()
    {
        LoadDiagnosticScene();
    }

    public void RunCalibration()
    {
        LoadCalibrationScene();
    }

    public void ReviewResults()
    {
        LoadResultsScene();
    }

    public void OpenSettings()
    {
        ChangeState(GameState.Settings);
    }

    public void PauseApplication()
    {
        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeApplication()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.MainMenu);
    }

    public void QuitApplication()
    {
        Debug.Log("Quitting VR Diagnostic Application...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
