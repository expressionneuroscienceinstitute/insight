using UnityEngine;
using System.IO;
using System.Text;
using System;

public class EyeGazeLogger : MonoBehaviour
{
    [Tooltip("Reference to the OVREyeGaze component on the left eye.")]
    public OVREyeGaze leftEyeGaze;

    [Tooltip("Reference to the OVREyeGaze component on the right eye.")]
    public OVREyeGaze rightEyeGaze;

    [Tooltip("The target space that holds the current fixation targets.")]
    public FollowHMD targetSpace;

    [Tooltip("Time interval (in seconds) to log the eye gaze data.")]
    public float logInterval = 0.1f;

    [Tooltip("Participant Name or File Prefix")]
    public string filePrefix = "insight";

    private float clock = 0f;
    private float logIntervaltimer = 0f;
    private string logFilePath;
    private readonly StringBuilder logData = new StringBuilder(); // Use StringBuilder for efficient string concatenation.
    private bool hasHeaderWritten = false;

    // Struct to store eye gaze data.
    public struct GazeData
    {
        public float timestamp;
        public Vector3 leftEyePos;
        public Quaternion leftEyeRot;
        public Vector3 leftEyeDir;
        public Vector3 rightEyePos;
        public Quaternion rightEyeRot;
        public Vector3 rightEyeDir;
        public Vector3 targetPos;
    }

    void Start()
    {
        // Validate inputs
        if (leftEyeGaze == null || rightEyeGaze == null)
        {
            Debug.LogError("leftEyeGaze or rightEyeGaze not assigned!");
            return;
        }

        // Define the log file path. On Windows, Application.persistentDataPath is writable.
        string fileSuffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"{filePrefix}_eyegaze_log_{fileSuffix}.csv");
    }

    void Update()
    {
        clock += Time.deltaTime;
        logIntervaltimer += Time.deltaTime;
        if (logIntervaltimer >= logInterval)
        {
            LogEyeGaze();
            logIntervaltimer = 0f;
        }
    }

    private void LogEyeGaze()
    {
        // Check if eye tracking is enabled and both eye gaze objects are not null.
        if (leftEyeGaze != null && rightEyeGaze != null && leftEyeGaze.EyeTrackingEnabled && rightEyeGaze.EyeTrackingEnabled)
        {
            GazeData data = new()
            {
                timestamp = clock,
                leftEyePos = leftEyeGaze.transform.position,
                leftEyeRot = leftEyeGaze.transform.rotation,
                leftEyeDir = leftEyeGaze.transform.forward,
                rightEyePos = rightEyeGaze.transform.position,
                rightEyeRot = rightEyeGaze.transform.rotation,
                rightEyeDir = rightEyeGaze.transform.forward,
                targetPos = targetSpace != null ? targetSpace.transform.position : Vector3.zero
            };

            AppendDataToLog(data);
        }
        else
        {
            Debug.LogWarning("Eye tracking not enabled or eyeGaze object is null.");
        }
    }

    private void AppendDataToLog(GazeData data)
    {
        // Check if the header has already been written.
        if (!hasHeaderWritten)
        {
            //Write header if not already written
            logData.AppendLine("Timestamp,LeftEyePosX,LeftEyePosY,LeftEyePosZ,LeftEyeRotX,LeftEyeRotY,LeftEyeRotZ,LeftEyeRotW,LeftEyeDirX,LeftEyeDirY,LeftEyeDirZ,RightEyePosX,RightEyePosY,RightEyePosZ,RightEyeRotX,RightEyeRotY,RightEyeRotZ,RightEyeRotW,RightEyeDirX,RightEyeDirY,RightEyeDirZ,TargetCenterX,TargetCenterY,TargetCenterZ");
            hasHeaderWritten = true;
        }

        // Append the data in CSV format.
        logData.AppendLine($"{data.timestamp},{data.leftEyePos.x},{data.leftEyePos.y},{data.leftEyePos.z},{data.leftEyeRot.x},{data.leftEyeRot.y},{data.leftEyeRot.z},{data.leftEyeRot.w},{data.leftEyeDir.x},{data.leftEyeDir.y},{data.leftEyeDir.z},{data.rightEyePos.x},{data.rightEyePos.y},{data.rightEyePos.z},{data.rightEyeRot.x},{data.rightEyeRot.y},{data.rightEyeRot.z},{data.rightEyeRot.w},{data.rightEyeDir.x},{data.rightEyeDir.y},{data.rightEyeDir.z},{data.targetPos.x},{data.targetPos.y},{data.targetPos.z}");

    }


    //call this to finish logging
    private void OnDestroy()
    {
        //check if the file already exists.
        if (File.Exists(logFilePath))
        {
            Debug.LogError("eyegaze log already exists");
            return;
        }

        // Write the accumulated data to the file.
        try
        {
            File.WriteAllText(logFilePath, logData.ToString());
            Debug.Log("Eye gaze log written to: " + logFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error writing eye gaze log: " + ex.Message);
        }
    }
}
