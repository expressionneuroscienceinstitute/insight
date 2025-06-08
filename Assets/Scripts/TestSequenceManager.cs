using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

public class TestSequenceManager : MonoBehaviour
{
    public enum Stage
    {
        Idle,
        AlignBaseline,
        Dissociate,
        MeasureDrift,
        ReAlignPeripheral,
        FineCheckIterate,
        Complete,
        Error
    }

    [Serializable]
    public struct GazeData
    {
        public Vector3 leftDir;
        public Vector3 rightDir;
        public double time;
    }

    [Serializable]
    public struct SessionLogEntry
    {
        public Stage stage;
        public double time;
        public Vector3 leftDir;
        public Vector3 rightDir;
        public Vector2 currentOffsetDeg;
    }

    [SerializeField] private StimulusController stim;
    [SerializeField] private float settleVelDegPerSec = 0.2f;
    [SerializeField] private float settleDuration = 1.0f;
    [SerializeField] private float driftStopThreshDeg = 0.05f;
    [SerializeField] private float driftStopWindow = 0.5f;
    [SerializeField] private float residualCutoffDeg = 0.1f;
    [SerializeField] private int maxIterations = 6;
    [SerializeField] private float virtualDistance = 6f;
    [SerializeField] private bool dominantIsLeft = true;

    private Stage currentStage = Stage.Idle;
    private List<SessionLogEntry> log = new List<SessionLogEntry>();
    private GazeData currentGaze;
    private Vector3 alignedPoseLeft;
    private Vector3 alignedPoseRight;
    private Vector3 phoriaPoseLeft;
    private Vector3 phoriaPoseRight;
    private Vector2 dissociatedPhoria;
    private Vector2 finalOffsetDeg;

    private Coroutine testCoroutine;
    private Queue<Vector2> velocityHistory = new Queue<Vector2>();
    private Queue<Vector2> positionHistory = new Queue<Vector2>();

    public event Action<Stage> OnStageChanged;

    private Stage CurrentStage
    {
        get => currentStage;
        set
        {
            if (currentStage != value)
            {
                currentStage = value;
                OnStageChanged?.Invoke(currentStage);
            }
        }
    }

    private void Update()
    {
        // Update eye gaze data every frame
        UpdateGazeData();

        // Add to log
        if (currentStage != Stage.Idle && currentStage != Stage.Complete && currentStage != Stage.Error)
        {
            log.Add(new SessionLogEntry
            {
                stage = currentStage,
                time = currentGaze.time,
                leftDir = currentGaze.leftDir,
                rightDir = currentGaze.rightDir,
                currentOffsetDeg = stim.CurrentOffsetDeg
            });
        }
    }

    public void BeginTest()
    {
        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
        }

        log.Clear();
        CurrentStage = Stage.Idle;
        testCoroutine = StartCoroutine(RunTest());
    }

    public float GetFinalHorizPrismDeg()
    {
        return finalOffsetDeg.x;
    }

    public float GetFinalVertPrismDeg()
    {
        return finalOffsetDeg.y;
    }

    private IEnumerator RunTest()
    {
        // Stage 1: Align Baseline
        CurrentStage = Stage.AlignBaseline;
        stim.ShowCentralCrossBothEyes();

        // Wait for eye movement to settle
        yield return WaitForEyesToSettle();

        // Store aligned pose
        alignedPoseLeft = currentGaze.leftDir;
        alignedPoseRight = currentGaze.rightDir;

        // Stage 2: Dissociate
        CurrentStage = Stage.Dissociate;
        stim.ShowCrossDominantEye_BlankOther();

        // Wait for covered eye to stop drifting
        yield return WaitForDriftToStop();

        // Store phoria pose
        phoriaPoseLeft = currentGaze.leftDir;
        phoriaPoseRight = currentGaze.rightDir;

        // Stage 3: Measure Drift
        CurrentStage = Stage.MeasureDrift;

        // Calculate drift between aligned and phoria poses
        Vector3 coveredEyeAligned = DominantIsLeft() ? alignedPoseRight : alignedPoseLeft;
        Vector3 coveredEyePhoria = DominantIsLeft() ? phoriaPoseRight : phoriaPoseLeft;
        
        float horizontalDrift = AngleBetweenDeg(
            new Vector3(coveredEyeAligned.x, 0, coveredEyeAligned.z),
            new Vector3(coveredEyePhoria.x, 0, coveredEyePhoria.z));
        
        // Determine sign of horizontal drift
        if (Vector3.Cross(new Vector3(coveredEyeAligned.x, 0, coveredEyeAligned.z), 
                          new Vector3(coveredEyePhoria.x, 0, coveredEyePhoria.z)).y < 0)
        {
            horizontalDrift = -horizontalDrift;
        }
        
        float verticalDrift = AngleBetweenDeg(
            new Vector3(0, coveredEyeAligned.y, coveredEyeAligned.z),
            new Vector3(0, coveredEyePhoria.y, coveredEyePhoria.z));
        
        // Determine sign of vertical drift
        if (Vector3.Cross(new Vector3(0, coveredEyeAligned.y, coveredEyeAligned.z), 
                          new Vector3(0, coveredEyePhoria.y, coveredEyePhoria.z)).x > 0)
        {
            verticalDrift = -verticalDrift;
        }
        
        dissociatedPhoria = new Vector2(horizontalDrift, verticalDrift);
        
        // Stage 4: ReAlign Peripheral
        CurrentStage = Stage.ReAlignPeripheral;
        stim.ApplyOffsetDeg(-dissociatedPhoria); // Negate drift
        stim.ShowBigRingBothEyes();
        
        // Wait for 1 second
        yield return new WaitForSeconds(1.0f);
        
        // Stage 5: Fine Check Iterate
        CurrentStage = Stage.FineCheckIterate;
        int iterations = 0;
        Vector2 residualError;
        
        do
        {
            residualError = MeasureResidual();
            
            if (Mathf.Abs(residualError.x) <= residualCutoffDeg && 
                Mathf.Abs(residualError.y) <= residualCutoffDeg)
            {
                break;
            }
            
            stim.ApplyOffsetDeg(stim.CurrentOffsetDeg - residualError);
            iterations++;
            
            yield return new WaitForSeconds(0.5f);
            
        } while (iterations < maxIterations);
        
        // Stage 6: Complete
        CurrentStage = Stage.Complete;
        finalOffsetDeg = stim.CurrentOffsetDeg;
        
        // Save log to JSON
        SaveLogToJson();
        
        yield break;
    }

    private IEnumerator WaitForEyesToSettle()
    {
        float stableTime = 0f;
        
        while (stableTime < settleDuration)
        {
            if (EyeVelocityDegPerSec().magnitude < settleVelDegPerSec)
            {
                stableTime += Time.deltaTime;
            }
            else
            {
                stableTime = 0f;
            }
            
            yield return null;
        }
    }

    private IEnumerator WaitForDriftToStop()
    {
        velocityHistory.Clear();
        positionHistory.Clear();
        float timeWaiting = 0f;
        
        while (timeWaiting < driftStopWindow)
        {
            Vector2 currentPose = CurrentPoseDeg();
            Vector2 currentVelocity = EyeVelocityDegPerSec();
            
            // Add to history
            positionHistory.Enqueue(currentPose);
            velocityHistory.Enqueue(currentVelocity);
            
            // Remove old entries
            while (positionHistory.Count > 0 && 
                  Time.time - (currentGaze.time - positionHistory.Peek().x) > driftStopWindow)
            {
                positionHistory.Dequeue();
                velocityHistory.Dequeue();
            }
            
            // Check if velocity is below threshold
            bool velocityStable = true;
            foreach (Vector2 vel in velocityHistory)
            {
                if (vel.magnitude > settleVelDegPerSec)
                {
                    velocityStable = false;
                    break;
                }
            }
            
            // Check if position change is below threshold
            bool positionStable = true;
            if (positionHistory.Count > 1)
            {
                Vector2 oldestPos = new Vector2();
                bool first = true;
                
                foreach (Vector2 pos in positionHistory)
                {
                    if (first)
                    {
                        oldestPos = pos;
                        first = false;
                    }
                    else if (Vector2.Distance(pos, oldestPos) > driftStopThreshDeg)
                    {
                        positionStable = false;
                        break;
                    }
                }
            }
            
            if (velocityStable && positionStable)
            {
                timeWaiting += Time.deltaTime;
            }
            else
            {
                timeWaiting = 0f;
            }
            
            yield return null;
        }
    }

    private Vector2 MeasureResidual()
    {
        // Show dot in left eye
        // TODO: Add proper stimulus for left eye dot
        
        // Wait for 0.5 seconds
        WaitForSeconds(0.5f);
        
        Vector3 leftEyeDotPos = currentGaze.leftDir;
        
        // Show dot in right eye
        // TODO: Add proper stimulus for right eye dot
        
        // Wait for 0.5 seconds
        WaitForSeconds(0.5f);
        
        Vector3 rightEyeDotPos = currentGaze.rightDir;
        
        // Calculate residual error
        float horizontalResidual = AngleBetweenDeg(
            new Vector3(leftEyeDotPos.x, 0, leftEyeDotPos.z),
            new Vector3(rightEyeDotPos.x, 0, rightEyeDotPos.z));
        
        // Determine sign
        if (Vector3.Cross(new Vector3(leftEyeDotPos.x, 0, leftEyeDotPos.z), 
                          new Vector3(rightEyeDotPos.x, 0, rightEyeDotPos.z)).y < 0)
        {
            horizontalResidual = -horizontalResidual;
        }
        
        float verticalResidual = AngleBetweenDeg(
            new Vector3(0, leftEyeDotPos.y, leftEyeDotPos.z),
            new Vector3(0, rightEyeDotPos.y, rightEyeDotPos.z));
        
        // Determine sign
        if (Vector3.Cross(new Vector3(0, leftEyeDotPos.y, leftEyeDotPos.z), 
                          new Vector3(0, rightEyeDotPos.y, rightEyeDotPos.z)).x > 0)
        {
            verticalResidual = -verticalResidual;
        }
        
        return new Vector2(horizontalResidual, verticalResidual);
    }

    private void WaitForSeconds(float seconds)
    {
        float startTime = Time.time;
        while (Time.time - startTime < seconds)
        {
            // Just a non-coroutine version for use in regular methods
        }
    }

    private Vector2 CurrentPoseDeg()
    {
        // Return horizontal and vertical angles of covered eye relative to forward
        Vector3 coveredEyeDir = DominantIsLeft() ? currentGaze.rightDir : currentGaze.leftDir;
        
        float horizontalAngle = AngleBetweenDeg(
            new Vector3(0, 0, 1),
            new Vector3(coveredEyeDir.x, 0, coveredEyeDir.z));
        
        // Determine sign
        if (coveredEyeDir.x < 0)
        {
            horizontalAngle = -horizontalAngle;
        }
        
        float verticalAngle = AngleBetweenDeg(
            new Vector3(0, 0, 1),
            new Vector3(0, coveredEyeDir.y, coveredEyeDir.z));
        
        // Determine sign
        if (coveredEyeDir.y > 0)
        {
            verticalAngle = -verticalAngle;
        }
        
        return new Vector2(horizontalAngle, verticalAngle);
    }

    private Vector2 EyeVelocityDegPerSec()
    {
        // Calculate velocity based on change in position over time
        // This is a simplified implementation - in practice, you'd want to track
        // multiple frames and compute a running average
        
        if (positionHistory.Count < 2)
            return Vector2.zero;
        
        Vector2 oldestPos = positionHistory.Peek();
        Vector2 newestPos = CurrentPoseDeg();
        float timeDiff = (float)(currentGaze.time - (currentGaze.time - velocityHistory.Count * Time.deltaTime));
        
        if (timeDiff <= 0)
            return Vector2.zero;
            
        return (newestPos - oldestPos) / timeDiff;
    }

    private float AngleBetweenDeg(Vector3 a, Vector3 b)
    {
        return Vector3.Angle(a, b);
    }

    private bool DominantIsLeft()
    {
        return dominantIsLeft;
    }

    private void UpdateGazeData()
    {
        // In a real implementation, this would get data from the eye tracker
        // For now, mock implementation
        
        currentGaze = new GazeData
        {
            leftDir = Camera.main.transform.forward,  // Replace with actual eye tracking data
            rightDir = Camera.main.transform.forward, // Replace with actual eye tracking data
            time = Time.time
        };
    }

    private void SaveLogToJson()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            string filename = $"session-{timestamp}.json";
            string path = Path.Combine(Application.persistentDataPath, filename);
            
            string json = JsonUtility.ToJson(new SessionLogWrapper { entries = log.ToArray() }, true);
            File.WriteAllText(path, json);
            
            Debug.Log($"Log saved to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save log: {e.Message}");
            CurrentStage = Stage.Error;
        }
    }

    [Serializable]
    private class SessionLogWrapper
    {
        public SessionLogEntry[] entries;
    }
} 