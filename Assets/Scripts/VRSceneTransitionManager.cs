using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR Scene Transition Manager with Addressables support
/// Handles async scene loading with loading screens and error handling
/// </summary>
public class VRSceneTransitionManager : MonoBehaviour
{
    public static VRSceneTransitionManager Instance { get; private set; }

    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreenCanvas;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TextMeshProUGUI loadingStatusText;
    [SerializeField] private TextMeshProUGUI loadingPercentageText;
    
    [Header("VR Loading Feedback")]
    [SerializeField] private GameObject vrLoadingIndicator;
    [SerializeField] private float loadingIndicatorRotationSpeed = 90f;
    
    [Header("Error Handling")]
    [SerializeField] private GameObject errorPopup;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private float errorDisplayDuration = 5f;

    // Loading state
    private bool isLoading = false;
    private AsyncOperationHandle<SceneInstance> currentLoadOperation;
    private Coroutine loadingCoroutine;

    // Events
    public System.Action<float> OnLoadingProgressChanged;
    public System.Action<string> OnLoadingStatusChanged;
    public System.Action OnLoadingStarted;
    public System.Action OnLoadingCompleted;
    public System.Action<string> OnLoadingError;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLoadingScreen();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLoadingScreen()
    {
        // Ensure loading screen is initially hidden
        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(false);
        }
        
        if (errorPopup != null)
        {
            errorPopup.SetActive(false);
        }
    }

    void Update()
    {
        // Animate VR loading indicator
        if (isLoading && vrLoadingIndicator != null && vrLoadingIndicator.activeInHierarchy)
        {
            vrLoadingIndicator.transform.Rotate(0, 0, loadingIndicatorRotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Load scene using Addressables with fallback to SceneManager
    /// </summary>
    public void LoadSceneAsync(AssetReference sceneReference, string fallbackSceneName, System.Action onComplete = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("Scene transition already in progress!");
            return;
        }

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(LoadSceneCoroutine(sceneReference, fallbackSceneName, onComplete));
    }

    /// <summary>
    /// Load scene using traditional SceneManager
    /// </summary>
    public void LoadSceneAsync(string sceneName, System.Action onComplete = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("Scene transition already in progress!");
            return;
        }

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(LoadSceneTraditionalCoroutine(sceneName, onComplete));
    }

    private IEnumerator LoadSceneCoroutine(AssetReference sceneReference, string fallbackSceneName, System.Action onComplete)
    {
        isLoading = true;
        ShowLoadingScreen("Preparing to load scene...");
        OnLoadingStarted?.Invoke();

        // Small delay to ensure loading screen is visible
        yield return new WaitForSeconds(0.1f);

        bool loadedSuccessfully = false;

        // Try Addressables first
        if (sceneReference != null && sceneReference.RuntimeKeyIsValid())
        {
            UpdateLoadingStatus("Loading scene via Addressables...");
            
            // Release previous scene if exists
            if (currentLoadOperation.IsValid())
            {
                UpdateLoadingStatus("Releasing previous scene...");
                Addressables.ReleaseInstance(currentLoadOperation);
                yield return new WaitForSeconds(0.1f);
            }

            // Load new scene
            currentLoadOperation = Addressables.LoadSceneAsync(sceneReference, LoadSceneMode.Single);
            
            while (!currentLoadOperation.IsDone)
            {
                float progress = currentLoadOperation.PercentComplete;
                UpdateLoadingProgress(progress);
                UpdateLoadingStatus($"Loading scene... {Mathf.RoundToInt(progress * 100)}%");
                yield return null;
            }

            if (currentLoadOperation.Status == AsyncOperationStatus.Succeeded)
            {
                loadedSuccessfully = true;
                UpdateLoadingStatus("Scene loaded successfully!");
            }
            else
            {
                string errorMsg = $"Addressables loading failed: {currentLoadOperation.OperationException?.Message}";
                Debug.LogError(errorMsg);
                OnLoadingError?.Invoke(errorMsg);
                UpdateLoadingStatus("Addressables failed, trying fallback...");
            }
        }

        // Fallback to SceneManager if Addressables failed
        if (!loadedSuccessfully && !string.IsNullOrEmpty(fallbackSceneName))
        {
            UpdateLoadingStatus("Loading via SceneManager...");
            
            AsyncOperation fallbackOperation = SceneManager.LoadSceneAsync(fallbackSceneName, LoadSceneMode.Single);
            
            while (!fallbackOperation.isDone)
            {
                float progress = fallbackOperation.progress;
                UpdateLoadingProgress(progress);
                UpdateLoadingStatus($"Loading scene... {Mathf.RoundToInt(progress * 100)}%");
                yield return null;
            }
            
            loadedSuccessfully = true;
            UpdateLoadingStatus("Scene loaded via fallback!");
        }

        if (loadedSuccessfully)
        {
            UpdateLoadingProgress(1.0f);
            UpdateLoadingStatus("Loading complete!");
            yield return new WaitForSeconds(0.5f); // Brief pause to show completion
            
            HideLoadingScreen();
            OnLoadingCompleted?.Invoke();
            onComplete?.Invoke();
        }
        else
        {
            string errorMsg = "Failed to load scene with both Addressables and SceneManager!";
            Debug.LogError(errorMsg);
            ShowError(errorMsg);
            HideLoadingScreen();
            OnLoadingError?.Invoke(errorMsg);
        }

        isLoading = false;
    }

    private IEnumerator LoadSceneTraditionalCoroutine(string sceneName, System.Action onComplete)
    {
        isLoading = true;
        ShowLoadingScreen($"Loading {sceneName}...");
        OnLoadingStarted?.Invoke();

        yield return new WaitForSeconds(0.1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        
        while (!operation.isDone)
        {
            float progress = operation.progress;
            UpdateLoadingProgress(progress);
            UpdateLoadingStatus($"Loading {sceneName}... {Mathf.RoundToInt(progress * 100)}%");
            yield return null;
        }

        UpdateLoadingProgress(1.0f);
        UpdateLoadingStatus("Loading complete!");
        yield return new WaitForSeconds(0.5f);

        HideLoadingScreen();
        OnLoadingCompleted?.Invoke();
        onComplete?.Invoke();

        isLoading = false;
    }

    private void ShowLoadingScreen(string initialStatus)
    {
        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(true);
        }

        if (vrLoadingIndicator != null)
        {
            vrLoadingIndicator.SetActive(true);
        }

        UpdateLoadingProgress(0f);
        UpdateLoadingStatus(initialStatus);
    }

    private void HideLoadingScreen()
    {
        if (loadingScreenCanvas != null)
        {
            loadingScreenCanvas.SetActive(false);
        }

        if (vrLoadingIndicator != null)
        {
            vrLoadingIndicator.SetActive(false);
        }
    }

    private void UpdateLoadingProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = progress;
        }

        if (loadingPercentageText != null)
        {
            loadingPercentageText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        OnLoadingProgressChanged?.Invoke(progress);
    }

    private void UpdateLoadingStatus(string status)
    {
        if (loadingStatusText != null)
        {
            loadingStatusText.text = status;
        }

        OnLoadingStatusChanged?.Invoke(status);
        Debug.Log($"VR Scene Transition: {status}");
    }

    public void ShowError(string errorMessage)
    {
        if (errorPopup != null)
        {
            errorPopup.SetActive(true);
            
            if (errorMessageText != null)
            {
                errorMessageText.text = errorMessage;
            }

            StartCoroutine(HideErrorAfterDelay());
        }

        Debug.LogError($"VR Scene Transition Error: {errorMessage}");
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorDisplayDuration);
        
        if (errorPopup != null)
        {
            errorPopup.SetActive(false);
        }
    }

    public void HideError()
    {
        if (errorPopup != null)
        {
            errorPopup.SetActive(false);
        }
    }

    /// <summary>
    /// Check if currently loading a scene
    /// </summary>
    public bool IsLoading => isLoading;

    /// <summary>
    /// Cancel current loading operation
    /// </summary>
    public void CancelLoading()
    {
        if (isLoading && loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            
            if (currentLoadOperation.IsValid())
            {
                Addressables.ReleaseInstance(currentLoadOperation);
            }
            
            isLoading = false;
            HideLoadingScreen();
            UpdateLoadingStatus("Loading cancelled");
        }
    }

    void OnDestroy()
    {
        // Clean up any active operations
        if (currentLoadOperation.IsValid())
        {
            Addressables.ReleaseInstance(currentLoadOperation);
        }
    }
}