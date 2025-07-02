using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR Main Menu Controller for Diagnostic Vision Therapy Platform
/// Manages the main menu UI with calibration status and navigation
/// </summary>
public class VRMainMenu : MonoBehaviour
{
    [Header("Main Menu UI Elements")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI calibrationStatusText;
    
    [Header("Menu Buttons")]
    [SerializeField] private InteractableMenuItem runCalibrationButton;
    [SerializeField] private InteractableMenuItem startDiagnosticButton;
    [SerializeField] private InteractableMenuItem reviewResultsButton;
    [SerializeField] private InteractableMenuItem settingsButton;
    [SerializeField] private InteractableMenuItem quitButton;
    
    [Header("Button Text Components")]
    [SerializeField] private TextMeshProUGUI calibrationButtonText;
    [SerializeField] private TextMeshProUGUI diagnosticButtonText;
    [SerializeField] private TextMeshProUGUI resultsButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI quitButtonText;
    
    [Header("Visual Feedback")]
    [SerializeField] private Image calibrationStatusIndicator;
    [SerializeField] private Color calibratedColor = Color.green;
    [SerializeField] private Color notCalibratedColor = Color.red;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioSource buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonSelectSound;

    private bool isInitialized = false;

    void Start()
    {
        InitializeMenu();
    }

    void OnEnable()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
            GameManager.Instance.OnCalibrationStatusChanged += OnCalibrationStatusChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnCalibrationStatusChanged -= OnCalibrationStatusChanged;
        }
    }

    private void InitializeMenu()
    {
        if (isInitialized) return;

        // Set up title
        if (titleText != null)
        {
            titleText.text = "VR Diagnostic Vision Therapy";
        }

        // Set up button texts
        if (calibrationButtonText != null) calibrationButtonText.text = "Run Calibration";
        if (diagnosticButtonText != null) diagnosticButtonText.text = "Start Diagnostic";
        if (resultsButtonText != null) resultsButtonText.text = "Review Results";
        if (settingsButtonText != null) settingsButtonText.text = "Settings";
        if (quitButtonText != null) quitButtonText.text = "Quit";

        // Set up button event listeners
        SetupButtonEvents();
        
        // Update initial status
        UpdateCalibrationStatus();
        UpdateButtonStates();

        isInitialized = true;
    }

    private void SetupButtonEvents()
    {
        // Run Calibration Button
        if (runCalibrationButton != null)
        {
            runCalibrationButton.OnGazeSelect.RemoveAllListeners();
            runCalibrationButton.OnGazeSelect.AddListener(OnRunCalibrationClicked);
        }

        // Start Diagnostic Button
        if (startDiagnosticButton != null)
        {
            startDiagnosticButton.OnGazeSelect.RemoveAllListeners();
            startDiagnosticButton.OnGazeSelect.AddListener(OnStartDiagnosticClicked);
        }

        // Review Results Button
        if (reviewResultsButton != null)
        {
            reviewResultsButton.OnGazeSelect.RemoveAllListeners();
            reviewResultsButton.OnGazeSelect.AddListener(OnReviewResultsClicked);
        }

        // Settings Button
        if (settingsButton != null)
        {
            settingsButton.OnGazeSelect.RemoveAllListeners();
            settingsButton.OnGazeSelect.AddListener(OnSettingsClicked);
        }

        // Quit Button
        if (quitButton != null)
        {
            quitButton.OnGazeSelect.RemoveAllListeners();
            quitButton.OnGazeSelect.AddListener(OnQuitClicked);
        }
    }

    private void UpdateCalibrationStatus()
    {
        bool isCalibrated = GameManager.Instance != null && GameManager.Instance.IsCalibrated;
        
        // Update status text
        if (calibrationStatusText != null)
        {
            calibrationStatusText.text = isCalibrated ? "Calibration: ✓ Complete" : "Calibration: ⚠ Required";
            calibrationStatusText.color = isCalibrated ? calibratedColor : notCalibratedColor;
        }

        // Update status indicator
        if (calibrationStatusIndicator != null)
        {
            calibrationStatusIndicator.color = isCalibrated ? calibratedColor : notCalibratedColor;
        }
    }

    private void UpdateButtonStates()
    {
        bool isCalibrated = GameManager.Instance != null && GameManager.Instance.IsCalibrated;

        // Start Diagnostic button should only be active if calibrated
        if (startDiagnosticButton != null)
        {
            startDiagnosticButton.SetActive(isCalibrated);
            if (diagnosticButtonText != null)
            {
                diagnosticButtonText.color = isCalibrated ? Color.white : Color.gray;
                diagnosticButtonText.text = isCalibrated ? "Start Diagnostic" : "Start Diagnostic\n(Calibration Required)";
            }
        }

        // Calibration button text update
        if (calibrationButtonText != null)
        {
            calibrationButtonText.text = isCalibrated ? "Re-run Calibration" : "Run Calibration";
        }
    }

    #region Button Event Handlers
    
    public void OnRunCalibrationClicked()
    {
        PlayButtonSound(buttonSelectSound);
        Debug.Log("VR Main Menu: Run Calibration clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunCalibration();
        }
    }

    public void OnStartDiagnosticClicked()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsCalibrated)
        {
            Debug.LogWarning("Cannot start diagnostic without calibration!");
            return;
        }

        PlayButtonSound(buttonSelectSound);
        Debug.Log("VR Main Menu: Start Diagnostic clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartDiagnostic();
        }
    }

    public void OnReviewResultsClicked()
    {
        PlayButtonSound(buttonSelectSound);
        Debug.Log("VR Main Menu: Review Results clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReviewResults();
        }
    }

    public void OnSettingsClicked()
    {
        PlayButtonSound(buttonSelectSound);
        Debug.Log("VR Main Menu: Settings clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OpenSettings();
        }
    }

    public void OnQuitClicked()
    {
        PlayButtonSound(buttonSelectSound);
        Debug.Log("VR Main Menu: Quit clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitApplication();
        }
    }

    #endregion

    #region Event Callbacks

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        // Show/hide main menu based on state
        bool shouldShowMainMenu = newState == GameManager.GameState.MainMenu;
        
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(shouldShowMainMenu);
        }
    }

    private void OnCalibrationStatusChanged(bool isCalibrated)
    {
        UpdateCalibrationStatus();
        UpdateButtonStates();
    }

    #endregion

    #region Audio Helpers

    private void PlayButtonSound(AudioClip clip)
    {
        if (buttonClickSound != null && clip != null)
        {
            buttonClickSound.PlayOneShot(clip);
        }
    }

    #endregion

    #region Legacy Support (for backward compatibility)
    
    [System.Obsolete("Use OnStartDiagnosticClicked instead")]
    public void StartGame()
    {
        OnStartDiagnosticClicked();
    }

    [System.Obsolete("Use OnQuitClicked instead")]
    public void ExitGame()
    {
        OnQuitClicked();
    }

    public void HideMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
        }
    }

    public void ShowMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
        }
        UpdateCalibrationStatus();
        UpdateButtonStates();
    }

    #endregion
}
