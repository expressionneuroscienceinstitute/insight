using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;

public class TestSequenceManagerTests
{
    private GameObject testGameObject;
    private TestSequenceManager manager;
    private StimulusController stimController;
    private MockEyeTracker mockEyeTracker;

    [SetUp]
    public void Setup()
    {
        // Create GameObjects
        testGameObject = new GameObject("TestSequenceManager");
        manager = testGameObject.AddComponent<TestSequenceManager>();
        
        GameObject stimObject = new GameObject("StimulusController");
        stimController = stimObject.AddComponent<StimulusController>();
        
        GameObject leftQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftQuad.name = "LeftQuad";
        leftQuad.transform.parent = stimObject.transform;
        
        GameObject rightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightQuad.name = "RightQuad";
        rightQuad.transform.parent = stimObject.transform;
        
        // Set up layers
        CreateLayer("LeftOnly");
        CreateLayer("RightOnly");
        
        leftQuad.layer = LayerMask.NameToLayer("LeftOnly");
        rightQuad.layer = LayerMask.NameToLayer("RightOnly");
        
        // Create materials
        Material crossMat = new Material(Shader.Find("Unlit/Color"));
        crossMat.color = Color.white;
        
        Material blankMat = new Material(Shader.Find("Unlit/Color"));
        blankMat.color = Color.black;
        
        Material ringMat = new Material(Shader.Find("Unlit/Color"));
        ringMat.color = Color.gray;
        
        Material dotMat = new Material(Shader.Find("Unlit/Color"));
        dotMat.color = Color.red;
        
        // Set up reflection to set private fields in StimulusController
        SetPrivateField(stimController, "leftEyeQuad", leftQuad);
        SetPrivateField(stimController, "rightEyeQuad", rightQuad);
        SetPrivateField(stimController, "crossMaterial", crossMat);
        SetPrivateField(stimController, "blankMaterial", blankMat);
        SetPrivateField(stimController, "bigRingMaterial", ringMat);
        SetPrivateField(stimController, "dotMaterial", dotMat);
        
        // Add MockEyeTracker
        mockEyeTracker = testGameObject.AddComponent<MockEyeTracker>();
        
        // Set up TestSequenceManager
        SetPrivateField(manager, "stim", stimController);
    }

    [TearDown]
    public void TearDown()
    {
        GameObject.DestroyImmediate(testGameObject);
    }

    [UnityTest]
    public IEnumerator TestSequenceManager_MeasuresCorrectPhoria()
    {
        // Set up test data - simulate 3-degree outward drift
        List<TestSequenceManager.GazeData> gazeSequence = new List<TestSequenceManager.GazeData>();
        
        // Setup aligned gaze - both eyes looking straight ahead
        TestSequenceManager.GazeData alignedGaze = new TestSequenceManager.GazeData
        {
            leftDir = Vector3.forward,
            rightDir = Vector3.forward,
            time = 0
        };
        
        // Setup drifted gaze - right eye drifts 3 degrees outward (to the right)
        Vector3 driftedRightDir = Quaternion.Euler(0, 3, 0) * Vector3.forward;
        TestSequenceManager.GazeData driftedGaze = new TestSequenceManager.GazeData
        {
            leftDir = Vector3.forward,
            rightDir = driftedRightDir,
            time = 5
        };
        
        // Add to sequence
        gazeSequence.Add(alignedGaze); // Will be used during alignment phase
        gazeSequence.Add(alignedGaze); // Repeat for stability
        gazeSequence.Add(driftedGaze); // Will be used during dissociation phase
        gazeSequence.Add(driftedGaze); // Repeat for stability
        
        // Set up mock eye tracker to replay this sequence
        mockEyeTracker.SetGazeSequence(gazeSequence);
        
        // Inject mock eye tracker into test sequence manager
        SetPrivateField(manager, "eyeTracker", mockEyeTracker);
        SetPrivateField(manager, "settleVelDegPerSec", 10f); // Make it easy to detect stability
        SetPrivateField(manager, "settleDuration", 0.1f);    // Shorter for testing
        SetPrivateField(manager, "driftStopWindow", 0.1f);   // Shorter for testing
        SetPrivateField(manager, "dominantIsLeft", true);    // Left eye is dominant
        
        // Start test
        bool testComplete = false;
        manager.OnStageChanged += (stage) => {
            if (stage == TestSequenceManager.Stage.Complete)
            {
                testComplete = true;
            }
        };
        
        manager.BeginTest();
        
        // Wait for test to complete or timeout
        float startTime = Time.time;
        while (!testComplete && Time.time - startTime < 10f)
        {
            yield return null;
        }
        
        // Check results
        Assert.IsTrue(testComplete, "Test did not complete within expected time");
        
        // Horizontal correction should be around 3 degrees (the simulated drift)
        float horizCorrection = manager.GetFinalHorizPrismDeg();
        Assert.AreEqual(3f, horizCorrection, 0.2f, "Horizontal prism correction should match simulated drift");
        
        // Vertical correction should be around 0 (no vertical drift simulated)
        float vertCorrection = manager.GetFinalVertPrismDeg();
        Assert.AreEqual(0f, vertCorrection, 0.2f, "Vertical prism correction should be near zero");
    }

    private void CreateLayer(string layerName)
    {
        // Note: This doesn't actually create layers at runtime in Unity
        // In a real test environment, you'd need to set up the layers in the editor
        // This is just a placeholder for what would be needed in a real test
        Debug.Log($"Would create layer: {layerName} (if this were possible at runtime)");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogError($"Field {fieldName} not found on {obj.GetType().Name}");
        }
    }
}

public class MockEyeTracker : MonoBehaviour
{
    private List<TestSequenceManager.GazeData> gazeSequence = new List<TestSequenceManager.GazeData>();
    private int currentIndex = 0;
    private TestSequenceManager manager;

    private void Start()
    {
        manager = GetComponent<TestSequenceManager>();
    }

    public void SetGazeSequence(List<TestSequenceManager.GazeData> sequence)
    {
        gazeSequence = sequence;
        currentIndex = 0;
    }

    private void Update()
    {
        if (gazeSequence.Count == 0 || manager == null)
            return;

        // Get current gaze data based on test stage
        TestSequenceManager.GazeData currentGaze;
        
        if (currentIndex < gazeSequence.Count)
        {
            currentGaze = gazeSequence[currentIndex];
            
            // Inject gaze data into manager
            var updateGazeMethod = manager.GetType().GetMethod("UpdateGazeData", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (updateGazeMethod != null)
            {
                // Replace the actual gaze update with our mock data
                var field = manager.GetType().GetField("currentGaze", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (field != null)
                {
                    field.SetValue(manager, currentGaze);
                }
            }
            
            // Advance sequence based on manager's current stage
            var stageField = manager.GetType().GetField("currentStage", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (stageField != null)
            {
                var stage = (TestSequenceManager.Stage)stageField.GetValue(manager);
                
                // Advance to the next gaze data in specific stages
                if (stage == TestSequenceManager.Stage.AlignBaseline && currentIndex == 0)
                {
                    // Move to next aligned gaze after some time in alignment stage
                    if (Time.frameCount % 10 == 0) currentIndex = 1;
                }
                else if (stage == TestSequenceManager.Stage.Dissociate && currentIndex < 2)
                {
                    // Move to drifted gaze data in dissociation stage
                    if (Time.frameCount % 10 == 0) currentIndex = 2;
                }
                else if (stage == TestSequenceManager.Stage.MeasureDrift && currentIndex < 3)
                {
                    // Ensure we're using the most drifted gaze data by measure drift stage
                    currentIndex = 3;
                }
            }
        }
    }
} 