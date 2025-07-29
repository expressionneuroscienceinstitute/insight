using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple VR Main Menu for basic VR applications
/// Provides basic menu functionality without complex diagnostic features
/// </summary>
public class SimpleVRMenu : MonoBehaviour
{
    [Header("Menu UI Elements")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Menu Buttons")]
    [SerializeField] private InteractableMenuItem startExperienceButton;
    [SerializeField] private InteractableMenuItem settingsButton;
    [SerializeField] private InteractableMenuItem quitButton;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioSource buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonSelectSound;

    void Start()
    {
        InitializeMenu();
    }

    void InitializeMenu()
    {
        // Set title text
        if (titleText != null)
        {
            titleText.text = "VR Experience";
        }

        // Setup button events
        if (startExperienceButton != null)
        {
            startExperienceButton.OnGazeSelect.AddListener(OnStartExperienceClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.OnGazeSelect.AddListener(OnSettingsClicked);
        }

        if (quitButton != null)
        {
            quitButton.OnGazeSelect.AddListener(OnQuitClicked);
        }

        Debug.Log("Simple VR Menu initialized");
    }

    public void OnStartExperienceClicked()
    {
        Debug.Log("Start Experience clicked");
        PlayButtonSound();
        
        // Add your VR experience logic here
        // For now, just log that the experience would start
    }

    public void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
        PlayButtonSound();
        
        // Add settings panel logic here
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        PlayButtonSound();
        
        if (VRApplicationManager.Instance != null)
        {
            VRApplicationManager.Instance.QuitApplication();
        }
        else
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && buttonSelectSound != null)
        {
            buttonClickSound.PlayOneShot(buttonSelectSound);
        }
    }

    public void ShowMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }
    }

    public void HideMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
    }
}