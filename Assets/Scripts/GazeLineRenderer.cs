using UnityEngine;

public class GazeLineRenderer : MonoBehaviour
{
    [Tooltip("Reference to the OVREyeGaze component for the left eye.")]
    public OVREyeGaze leftEyeGaze;

    [Tooltip("Reference to the OVREyeGaze component for the right eye.")]
    public OVREyeGaze rightEyeGaze;

    private LineRenderer leftLineRenderer;
    private LineRenderer rightLineRenderer;

    private LineRenderer calLeftLineRenderer;
    private LineRenderer calRightLineRenderer;

    [Tooltip("How far the gaze rays should extend.")]
    public float maxDistance = 6f;

    [Tooltip("Prefab for the convergence point marker.")]
    public GameObject convergenceMarkerPrefab;

    private GameObject convergenceMarker;

    // Flag to indicate if this instance is controlled externally (e.g., by replay)
    private bool isExternallyControlled = false;

    void Start()
    {
        // Check if controlled by replay script which sets eye gaze components to null
        isExternallyControlled = leftEyeGaze == null && rightEyeGaze == null;

        if (!isExternallyControlled)
        {
            InitializeEyeGazeComponents();
        }
        CreateLineRenderers();
        CreateConvergenceMarker();

        // Initially hide calibrated lines if not calibrated yet or if externally controlled
        if (calLeftLineRenderer) calLeftLineRenderer.gameObject.SetActive(false);
        if (calRightLineRenderer) calRightLineRenderer.gameObject.SetActive(false);
    }

    private void InitializeEyeGazeComponents()
    {
        // No need to find components if they are explicitly set to null by replay
        if (leftEyeGaze == null && rightEyeGaze == null) return;

        if (leftEyeGaze == null)
        {
            leftEyeGaze = FindEyeGazeComponent("Left");
            if (leftEyeGaze == null) Debug.LogError("Left OVREyeGaze component not found in the scene.");
        }

        if (rightEyeGaze == null)
        {
            rightEyeGaze = FindEyeGazeComponent("Right");
            if (rightEyeGaze == null) Debug.LogError("Right OVREyeGaze component not found in the scene.");
        }
    }

    private OVREyeGaze FindEyeGazeComponent(string eyeName)
    {
        OVREyeGaze[] eyeGazeComponents = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None);
        foreach (OVREyeGaze eyeGaze in eyeGazeComponents)
        {
            if (eyeGaze.gameObject.name.ToLower().Contains(eyeName.ToLower()))
            {
                return eyeGaze;
            }
        }
        return null;
    }
    private void CreateLineRenderers()
    {
        // Create and configure the LineRenderer for the left eye.
        GameObject leftLineObject = new GameObject("LeftGazeLine");
        leftLineRenderer = leftLineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(leftLineRenderer, Color.green);
        leftLineRenderer.transform.parent = transform;

        // Create and configure the LineRenderer for the right eye.
        GameObject rightLineObject = new GameObject("RightGazeLine");
        rightLineRenderer = rightLineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(rightLineRenderer, Color.red);
        rightLineRenderer.transform.parent = transform;

        // Create and configure the LineRenderer for the calibrated eyes
        GameObject calLeftLineObject = new GameObject("CalibratedLeftGazeLine");
        calLeftLineRenderer = calLeftLineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(calLeftLineRenderer, Color.blue);
        calLeftLineRenderer.transform.parent = transform;

        GameObject calRightLineObject = new GameObject("CalibratedRightGazeLine");
        calRightLineRenderer = calRightLineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(calRightLineRenderer, Color.gray);
        calRightLineRenderer.transform.parent = transform;
    }
    private void ConfigureLineRenderer(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.0025f;
        lineRenderer.endWidth = 0.0025f;
        // Use a shared material for efficiency if possible, otherwise create new
        Material lineMaterial = new Material(Shader.Find("Unlit/Color"));
        lineMaterial.color = color;
        lineRenderer.material = lineMaterial;
        // Consider making the layer configurable or removing if not needed for replay
        lineRenderer.gameObject.layer = LayerMask.NameToLayer("Gaze LineRenders");
    }

    private void CreateConvergenceMarker()
    {
        if (convergenceMarkerPrefab == null)
        {
            Debug.LogWarning("Convergence Marker Prefab not assigned! Marker will not be shown.");
            return; // Don't log error, just warn and proceed without marker
        }

        convergenceMarker = Instantiate(convergenceMarkerPrefab);
        convergenceMarker.name = "ConvergenceMarker";
        // Consider making the layer configurable or removing if not needed for replay
        convergenceMarker.layer = LayerMask.NameToLayer("Gaze LineRenders");
        convergenceMarker.transform.parent = transform;
        convergenceMarker.SetActive(false); // Start inactive
    }

    void Update()
    {
        // Only run live update logic if not controlled externally
        if (isExternallyControlled) return;

        // Ensure eye gaze components are valid before proceeding
        if (leftEyeGaze == null || rightEyeGaze == null || !leftEyeGaze.EyeTrackingEnabled || !rightEyeGaze.EyeTrackingEnabled)
        {
            // Optionally hide visuals if eye tracking is lost/invalid
            if (leftLineRenderer) leftLineRenderer.gameObject.SetActive(false);
            if (rightLineRenderer) rightLineRenderer.gameObject.SetActive(false);
            if (calLeftLineRenderer) calLeftLineRenderer.gameObject.SetActive(false);
            if (calRightLineRenderer) calRightLineRenderer.gameObject.SetActive(false);
            if (convergenceMarker) convergenceMarker.SetActive(false);
            return;
        }

        // Ensure renderers are active
        if (leftLineRenderer) leftLineRenderer.gameObject.SetActive(true);
        if (rightLineRenderer) rightLineRenderer.gameObject.SetActive(true);


        bool isCalibrated = EyeCalibrationManager.Instance != null && EyeCalibrationManager.Instance.Calibrated;

        // Activate/Deactivate calibrated lines based on calibration status
        if (calLeftLineRenderer) calLeftLineRenderer.gameObject.SetActive(isCalibrated);
        if (calRightLineRenderer) calRightLineRenderer.gameObject.SetActive(isCalibrated);

        // draw *calibrated* rays from manager’s transforms
        if (isCalibrated)
        {
            VisualizeGazeRay(EyeCalibrationManager.Instance.LeftGazeTransform.position, EyeCalibrationManager.Instance.LeftGazeTransform.forward, calLeftLineRenderer);
            VisualizeGazeRay(EyeCalibrationManager.Instance.RightGazeTransform.position, EyeCalibrationManager.Instance.RightGazeTransform.forward, calRightLineRenderer);
        }

        // draw *raw* rays from OVREyeGaze
        VisualizeGazeRay(leftEyeGaze.transform.position, leftEyeGaze.transform.forward, leftLineRenderer);
        VisualizeGazeRay(rightEyeGaze.transform.position, rightEyeGaze.transform.forward, rightLineRenderer);

        VisualizeConvergence();
    }

    /// <summary>
    /// Updates the gaze visualization using externally provided data (e.g., from a replay).
    /// </summary>
    /// <param name="lPos">Left eye position.</param>
    /// <param name="lDir">Left eye direction (normalized).</param>
    /// <param name="rPos">Right eye position.</param>
    /// <param name="rDir">Right eye direction (normalized).</param>
    public void VisualizeExternal(Vector3 lPos, Vector3 lDir, Vector3 rPos, Vector3 rDir)
    {
        // Ensure this instance knows it's externally controlled
        isExternallyControlled = true;

        // Hide calibrated lines during external replay as we only have raw data
        if (calLeftLineRenderer) calLeftLineRenderer.gameObject.SetActive(false);
        if (calRightLineRenderer) calRightLineRenderer.gameObject.SetActive(false);

        // Visualize the provided raw gaze data
        if (leftLineRenderer)
        {
            leftLineRenderer.gameObject.SetActive(true);
            VisualizeGazeRay(lPos, lDir, leftLineRenderer);
        }
        if (rightLineRenderer)
        {
            rightLineRenderer.gameObject.SetActive(true);
            VisualizeGazeRay(rPos, rDir, rightLineRenderer);
        }

        // Visualize convergence based on the provided data
        VisualizeConvergenceExternal(lPos, lDir, rPos, rDir);
    }

    public void VisualizeExternalCalibrated(Vector3 lPos, Vector3 lDir,
                                         Vector3 rPos, Vector3 rDir)
    {
        isExternallyControlled = true;

        if (calLeftLineRenderer) calLeftLineRenderer.gameObject.SetActive(true);
        if (calRightLineRenderer) calRightLineRenderer.gameObject.SetActive(true);

        VisualizeGazeRay(lPos, lDir.normalized, calLeftLineRenderer);
        VisualizeGazeRay(rPos, rDir.normalized, calRightLineRenderer);
    }


    // Renamed VisualizeGaze to VisualizeGazeRay to be more specific
    private void VisualizeGazeRay(Vector3 origin, Vector3 direction, LineRenderer lr)
    {
        if (lr == null) return;
        lr.SetPosition(0, origin);
        direction.Normalize();
        lr.SetPosition(1, origin + direction * maxDistance);
    }

    // Visualize convergence based on live data
    private void VisualizeConvergence()
    {
        Transform leftSrc, rightSrc;
        bool useCalibrated = EyeCalibrationManager.Instance != null && EyeCalibrationManager.Instance.Calibrated;

        if (useCalibrated)
        {
            leftSrc = EyeCalibrationManager.Instance.LeftGazeTransform;
            rightSrc = EyeCalibrationManager.Instance.RightGazeTransform;
        }
        else
        {
            leftSrc = leftEyeGaze ? leftEyeGaze.transform : null;
            rightSrc = rightEyeGaze ? rightEyeGaze.transform : null;
        }

        // Use positions and directions for calculation
        if (leftSrc != null && rightSrc != null)
        {
            UpdateConvergenceMarker(leftSrc.position, leftSrc.forward, rightSrc.position, rightSrc.forward);
        }
        else
        {
            if (convergenceMarker) convergenceMarker.SetActive(false);
        }
    }

    // Visualize convergence based on external data
    private void VisualizeConvergenceExternal(Vector3 lPos, Vector3 lDir, Vector3 rPos, Vector3 rDir)
    {
        UpdateConvergenceMarker(lPos, lDir, rPos, rDir);
    }

    // Helper method to update the convergence marker position
    private void UpdateConvergenceMarker(Vector3 leftPos, Vector3 leftDir, Vector3 rightPos, Vector3 rightDir)
    {
        if (convergenceMarker == null) return; // Check if marker exists

        convergenceMarker.SetActive(true);
        Vector3 vergencePoint = GetVergencePoint(leftPos, leftDir.normalized, rightPos, rightDir.normalized); // Ensure directions are normalized
        convergenceMarker.transform.position = vergencePoint;
    }


    private Vector3 GetVergencePoint(Vector3 leftPos, Vector3 leftDir, Vector3 rightPos, Vector3 rightDir)
    {
        // Ensure directions are normalized before use
        leftDir.Normalize();
        rightDir.Normalize();

        Vector3 w0 = leftPos - rightPos;
        float a = Vector3.Dot(leftDir, leftDir); // Will be 1 if normalized
        float b = Vector3.Dot(leftDir, rightDir);
        float c = Vector3.Dot(rightDir, rightDir); // Will be 1 if normalized
        float d = Vector3.Dot(leftDir, w0);
        float e = Vector3.Dot(rightDir, w0);
        // Since a and c are 1 (normalized vectors), denominator simplifies
        // float denominator = (a * c) - (b * b);
        float denominator = 1f - (b * b);


        float tNonParallel, sNonParallel;

        // Check if lines are parallel or very close to it
        if (Mathf.Abs(denominator) < 1e-6f)
        {
            float t_approx = Vector3.Dot(w0, leftDir) / a; // Project w0 onto leftDir
            float s_approx = -Vector3.Dot(w0, rightDir) / c; // Project -w0 onto rightDir

            // Use the average distance along the rays
            float avg_dist = (t_approx - s_approx) * 0.5f; // Note: s_approx is distance along rightDir from rightPos

            // Clamp the distance to prevent extreme values if rays diverge quickly
            avg_dist = Mathf.Clamp(avg_dist, 0f, maxDistance * 2f);

            Vector3 pointOnLeft = leftPos + avg_dist * leftDir;
            Vector3 pointOnRight = rightPos + avg_dist * rightDir;
            return (pointOnLeft + pointOnRight) * 0.5f;

        }
        else // Lines are not parallel
        {
            tNonParallel = (b * e - /*c=1*/ d) / denominator; // Simplified c=1
            sNonParallel = (/*a=1*/ e - b * d) / denominator; // Simplified a=1
        }


        // Clamp t and s to prevent the point from being behind the eyes or extremely far away.
        // Consider clamping based on maxDistance or a larger reasonable value.
        const float maxConvergenceCalcDistance = 15f;
        tNonParallel = Mathf.Clamp(tNonParallel, 0f, maxConvergenceCalcDistance);
        sNonParallel = Mathf.Clamp(sNonParallel, 0f, maxConvergenceCalcDistance);

        Vector3 pointOnLeftNonParallel = leftPos + tNonParallel * leftDir;
        Vector3 pointOnRightNonParallel = rightPos + sNonParallel * rightDir;
        return (pointOnLeftNonParallel + pointOnRightNonParallel) * 0.5f;
    }
}
