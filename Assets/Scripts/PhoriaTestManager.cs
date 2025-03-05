using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PhoriaTestManager : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Array of target GameObjects (e.g., iPhone 25cm, Computer Monitor 44cm, TV 2m, Bird 6m). Their center will be used as the fixation point.")]
    public GameObject[] testTargets;
    [Tooltip("Time (in seconds) to display each target and collect gaze data.")]
    public float displayDuration = 30f;

    [Header("Eye Gaze Building Blocks")]
    [Tooltip("Reference to the EyeGaze building block on the LEFT eye.")]
    public OVREyeGaze leftEyeGaze;
    [Tooltip("Reference to the EyeGaze building block on the RIGHT eye.")]
    public OVREyeGaze rightEyeGaze;

    [Header("UI Settings")]
    [Tooltip("Reference to the Text UI element to display the results.")]
    public PhoriaTestResultsDisplay resultsDisplay;

    // Internal state
    public bool isTesting = false;
    public int currentTargetIndex = 0;
    private float timer = 0f;
    private List<float> phoriaSamples = new List<float>(); // Collected phoria values for the current target.
    private List<float> leftPhoriaSamples = new List<float>(); // Collected phoria values for the current target for the left eye.
    private List<float> rightPhoriaSamples = new List<float>(); // Collected phoria values for the current target for the right eye.
    private List<float> leftTropiaSamples = new List<float>();
    private List<float> rightTropiaSamples = new List<float>();
    private List<float> overallTropiaSamples = new List<float>();

    private List<string> diagnosticLogs = new List<string>();
    private Dictionary<string, float> targetPhoriaResults = new Dictionary<string, float>();
    private Dictionary<string, float> targetLeftPhoriaResults = new Dictionary<string, float>();
    private Dictionary<string, float> targetRightPhoriaResults = new Dictionary<string, float>();
    private Dictionary<string, float> targetLeftTropiaResults = new Dictionary<string, float>();
    private Dictionary<string, float> targetRightTropiaResults = new Dictionary<string, float>();
    private Dictionary<string, float> targetOverallTropiaResults = new Dictionary<string, float>();
    private Dictionary<string, EyeMisalignmentCalculator.MisalignmentType> targetMisalignmentType = new Dictionary<string, EyeMisalignmentCalculator.MisalignmentType>();

    private float emaAlpha = 0.1f; // Smoothing factor (adjust as needed)
    private float emaPhoria = 0f;
    private float emaLeftPhoria = 0f;
    private float emaRightPhoria = 0f;
    private float emaLeftTropia = 0f;
    private float emaRightTropia = 0f;
    private float emaOverallTropia = 0f;

    private Transform centerEyeAnchor;

    void Start()
    {
        // Activate only the first target and hide the others.
        for (int i = 0; i < testTargets.Length; i++)
        {
            testTargets[i].SetActive(i == currentTargetIndex);
        }
        timer = 0f;
        phoriaSamples.Clear();
        leftPhoriaSamples.Clear();
        rightPhoriaSamples.Clear();
        leftTropiaSamples.Clear();
        rightTropiaSamples.Clear();
        overallTropiaSamples.Clear();

        isTesting = true;

        OVRCameraRig cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            centerEyeAnchor = cameraRig.centerEyeAnchor;
            if (centerEyeAnchor == null)
            {
                Debug.LogError("CenterEyeAnchor not found in OVRCameraRig!");
            }
        }
        else
        {
            Debug.LogError("OVRCameraRig not found in the scene!");
        }
    }

    void Update()
    {
        if (leftEyeGaze == null || rightEyeGaze == null)
        {
            Debug.LogError("leftEyeGaze or rightEyeGaze not assigned!");
            return;
        }

        // When all targets have been tested, display final results and stop updating.
        if (currentTargetIndex >= testTargets.Length)
        {
            if (isTesting)
            {
                DisplayResults();
                isTesting = false;
            }
            return;
        }

        timer += Time.deltaTime;

        // Get each eye's position from its EyeGaze building block.
        Vector3 leftEyePos = leftEyeGaze.transform.position;
        Vector3 rightEyePos = rightEyeGaze.transform.position;

        // Retrieve each eye's normalized gaze direction.
        Vector3 leftGazeDir = leftEyeGaze.transform.forward.normalized;
        Vector3 rightGazeDir = rightEyeGaze.transform.forward.normalized;

        // Get the target center
        Vector3 targetCenter = testTargets[currentTargetIndex].transform.position;

        // Calculate misalignment using the EyeMisalignmentCalculator
        EyeMisalignmentCalculator.MisalignmentData misalignmentData;
        EyeMisalignmentCalculator.MisalignmentResult result = EyeMisalignmentCalculator.CalculateEyeMisalignment(leftEyePos, leftGazeDir, rightEyePos, rightGazeDir, targetCenter, out misalignmentData);
        if (result == null) return;

        // Apply EMA filtering
        phoriaSamples.Add(result.OverallPhoria);
        leftPhoriaSamples.Add(result.LeftPhoria);
        rightPhoriaSamples.Add(result.RightPhoria);
        leftTropiaSamples.Add(result.LeftTropia);
        rightTropiaSamples.Add(result.RightTropia);
        overallTropiaSamples.Add(result.OverallTropia);

        emaPhoria = emaAlpha * result.OverallPhoria + (1 - emaAlpha) * emaPhoria;
        emaLeftPhoria = emaAlpha * result.LeftPhoria + (1 - emaAlpha) * emaLeftPhoria;
        emaRightPhoria = emaAlpha * result.RightPhoria + (1 - emaAlpha) * emaRightPhoria;
        emaLeftTropia = emaAlpha * result.LeftTropia + (1-emaAlpha) * emaLeftTropia;
        emaRightTropia = emaAlpha * result.RightTropia + (1-emaAlpha) * emaRightTropia;
        emaOverallTropia = emaAlpha * result.OverallTropia + (1-emaAlpha) * emaOverallTropia;

        //After displayDuration seconds, average the collected samples and move to the next target.
        if (timer >= displayDuration)
        {
            float avgPhoria = emaPhoria;
            float avgLeftPhoria = emaLeftPhoria;
            float avgRightPhoria = emaRightPhoria;
            float avgLeftTropia = emaLeftTropia;
            float avgRightTropia = emaRightTropia;
            float avgOverallTropia = emaOverallTropia;

            // Save the average phoria result using the target's name.
            targetPhoriaResults[testTargets[currentTargetIndex].name] = avgPhoria;
            targetLeftPhoriaResults[testTargets[currentTargetIndex].name] = avgLeftPhoria;
            targetRightPhoriaResults[testTargets[currentTargetIndex].name] = avgRightPhoria;
            targetLeftTropiaResults[testTargets[currentTargetIndex].name] = avgLeftTropia;
            targetRightTropiaResults[testTargets[currentTargetIndex].name] = avgRightTropia;
            targetOverallTropiaResults[testTargets[currentTargetIndex].name] = avgOverallTropia;

            targetMisalignmentType[testTargets[currentTargetIndex].name] = result.MisalignmentType;


            Debug.Log($"Target: {testTargets[currentTargetIndex].name} |  Avg Phoria: {avgPhoria.ToString("F3")} diopters | Left Phoria: {avgLeftPhoria.ToString("F3")} diopters | Right Phoria: {avgRightPhoria.ToString("F3")} diopters | Left Tropia: {avgLeftTropia.ToString("F3")} | Right Tropia: {avgRightTropia.ToString("F3")} | Overall Tropia: {avgOverallTropia}");
            diagnosticLogs.Add($"Target: {testTargets[currentTargetIndex].name} |  Avg Phoria: {avgPhoria.ToString("F3")} diopters | Left Phoria: {avgLeftPhoria.ToString("F3")} diopters | Right Phoria: {avgRightPhoria.ToString("F3")} diopters | Left Tropia: {avgLeftTropia.ToString("F3")} | Right Tropia: {avgRightTropia.ToString("F3")} | Overall Tropia: {avgOverallTropia}");

            // Move to the next target.
            currentTargetIndex++;
            if (currentTargetIndex < testTargets.Length)
            {
                timer = 0f;
                phoriaSamples.Clear();
                leftPhoriaSamples.Clear();
                rightPhoriaSamples.Clear();
                leftTropiaSamples.Clear();
                rightTropiaSamples.Clear();
                overallTropiaSamples.Clear();
                // Activate only the current target.
                for (int i = 0; i < testTargets.Length; i++)
                {
                    testTargets[i].SetActive(i == currentTargetIndex);
                }
                return;
            }
        }
    }

    // Helper method to calculate average of a list
    private float CalculateAverage(List<float> list)
    {
        float sum = 0f;
        foreach (float item in list)
        {
            sum += item;
        }
        return list.Count > 0 ? sum / list.Count : 0f;
    }

    // Helper method to write diagnostic logs to a file.
    private void WriteDiagnosticsToFile()
    {
        // For Windows, Application.persistentDataPath is writable.
        string filePath = Path.Combine(Application.persistentDataPath, "diagnostic_log.txt");
        try
        {
            File.WriteAllLines(filePath, diagnosticLogs);
            Debug.Log("Diagnostic log written to: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to write diagnostic log: " + ex.Message);
        }
    }

    // This method displays the final phoria results in the console.
    void DisplayResults()
    {
        string results = "=== Phoria Test Results ===\n";
        foreach (var result in targetPhoriaResults)
        {
            string targetName = result.Key;
            float avgPhoria = result.Value;
            float leftPhoria = targetLeftPhoriaResults.ContainsKey(targetName) ? targetLeftPhoriaResults[targetName] : 0f;
            float rightPhoria = targetRightPhoriaResults.ContainsKey(targetName) ? targetRightPhoriaResults[targetName] : 0f;
            float leftTropia = targetLeftTropiaResults.ContainsKey(targetName) ? targetLeftTropiaResults[targetName] : 0f;
            float rightTropia = targetRightTropiaResults.ContainsKey(targetName) ? targetRightTropiaResults[targetName] : 0f;
            float overallTropia = targetOverallTropiaResults.ContainsKey(targetName) ? targetOverallTropiaResults[targetName] : 0f;
            EyeMisalignmentCalculator.MisalignmentType type = targetMisalignmentType.ContainsKey(targetName) ? targetMisalignmentType[targetName] : EyeMisalignmentCalculator.MisalignmentType.None;

            results += $"Target: {targetName} | Average Phoria: {avgPhoria:F3} diopters | Left Phoria: {leftPhoria:F3} diopters | Right Phoria: {rightPhoria:F3} diopters | Left Tropia: {leftTropia:F3} | Right Tropia: {rightTropia:F3} | Overall Tropia: {overallTropia:F3} | Misalignment Type: {type}\n";

            Debug.Log($"Target: {targetName} | Avg Phoria: {avgPhoria.ToString("F3")} diopters | Left Phoria: {leftPhoria.ToString("F3")} diopters | Right Phoria: {rightPhoria.ToString("F3")} diopters | Left Tropia: {leftTropia.ToString("F3")} | Right Tropia: {rightTropia.ToString("F3")} | Overall Tropia: {overallTropia:F3} | Misalignment Type: {type}");
        }

        WriteDiagnosticsToFile();

        if (resultsDisplay != null)
        {
            resultsDisplay.DisplayResults(results);
        }
        else
        {
            Debug.LogError("Results Display UI element is not assigned.");
        }
    }
}
