using UnityEngine;

/// <summary>
/// Simple VR Application Manager for basic VR experiences
/// Provides essential VR functionality without complex diagnostic features
/// </summary>
public class VRApplicationManager : MonoBehaviour
{
    // Singleton instance
    public static VRApplicationManager Instance { get; private set; }

    [Header("VR Settings")]
    [Tooltip("Enable/disable basic VR features")]
    public bool enableGazeInteraction = true;
    
    [Header("Application State")]
    public bool isApplicationPaused = false;

    // Events for basic VR functionality
    public System.Action OnApplicationPaused;
    public System.Action OnApplicationResumed;

    void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("VR Application Manager initialized");
    }

    /// <summary>
    /// Pause the VR application
    /// </summary>
    public void PauseApplication()
    {
        isApplicationPaused = true;
        Time.timeScale = 0f;
        OnApplicationPaused?.Invoke();
        Debug.Log("VR Application paused");
    }

    /// <summary>
    /// Resume the VR application
    /// </summary>
    public void ResumeApplication()
    {
        isApplicationPaused = false;
        Time.timeScale = 1f;
        OnApplicationResumed?.Invoke();
        Debug.Log("VR Application resumed");
    }

    /// <summary>
    /// Quit the VR application
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("Quitting VR Application");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}