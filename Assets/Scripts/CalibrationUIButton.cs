using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hooks a Unity UI Button to the EyeCalibrationManager in the scene.
/// </summary>
public class CalibrationUIButton : MonoBehaviour
{
    [SerializeField] private EyeCalibrationManager calibrationManager;
    [SerializeField] private Button startButton;

    void Awake()
    {
        // Fallbacks if you forgot to wire them in the Inspector
        if (calibrationManager == null)
            calibrationManager = FindFirstObjectByType<EyeCalibrationManager>();
        if (startButton == null)
            startButton = GetComponent<Button>();

        // Guard clauses
        if (calibrationManager == null || startButton == null)
        {
            Debug.LogError("CalibrationUIButton: Missing reference(s)");
            enabled = false;
            return;
        }

        // Hook it up
        startButton.onClick.AddListener(calibrationManager.BeginCalibration);

        // Keep button disabled after calibration starts
        calibrationManager.OnCalibrated += () => startButton.interactable = true;
        startButton.onClick.AddListener(() => startButton.interactable = false);
    }
}
