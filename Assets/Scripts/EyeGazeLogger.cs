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

    [Tooltip("Time interval (in seconds) to log the eye gaze data.")]
    public float logInterval = 0.1f;

    private float clock = 0f;
    private float logIntervaltimer = 0f;
    private string logFilePath;
    private StringBuilder logData = new StringBuilder(); // Use StringBuilder for efficient string concatenation.
    private bool hasHeaderWritten = false;

    // Struct to store eye gaze data.
    public struct GazeData
    {
        public float Timestamp;
        public Vector3 LeftEyePos;
        public Quaternion LeftEyeRot;
        public Vector3 LeftEyeDir;
        public Vector3 RightEyePos;
        public Quaternion RightEyeRot;
        public Vector3 RightEyeDir;
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
        string fileSuffix = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"eyegaze_log_{fileSuffix}.csv");
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
                Timestamp = clock,
                LeftEyePos = leftEyeGaze.transform.position,
                LeftEyeRot = leftEyeGaze.transform.rotation,
                LeftEyeDir = leftEyeGaze.transform.forward,
                RightEyePos = rightEyeGaze.transform.position,
                RightEyeRot = rightEyeGaze.transform.rotation,
                RightEyeDir = rightEyeGaze.transform.forward
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
        logData.AppendLine($"{data.Timestamp},{data.LeftEyePos.x},{data.LeftEyePos.y},{data.LeftEyePos.z},{data.LeftEyeRot.x},{data.LeftEyeRot.y},{data.LeftEyeRot.z},{data.LeftEyeRot.w},{data.LeftEyeDir.x},{data.LeftEyeDir.y},{data.LeftEyeDir.z},{data.RightEyePos.x},{data.RightEyePos.y},{data.RightEyePos.z},{data.RightEyeRot.x},{data.RightEyeRot.y},{data.RightEyeRot.z},{data.RightEyeRot.w},{data.RightEyeDir.x},{data.RightEyeDir.y},{data.RightEyeDir.z}");
    
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
