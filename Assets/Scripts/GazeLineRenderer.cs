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

    void Start()
    {
        InitializeEyeGazeComponents();
        CreateLineRenderers();
        CreateConvergenceMarker();
    }

    private void InitializeEyeGazeComponents()
    {
        if (leftEyeGaze == null)
        {
            leftEyeGaze = FindEyeGazeComponent("Left");
        }

        if (rightEyeGaze == null)
        {
            rightEyeGaze = FindEyeGazeComponent("Right");
        }

        if (leftEyeGaze == null)
        {
            Debug.LogError("Left OVREyeGaze component not found in the scene.");
        }

        if (rightEyeGaze == null)
        {
            Debug.LogError("Right OVREyeGaze component not found in the scene.");
        }
    }

    private OVREyeGaze FindEyeGazeComponent(string eyeName)
    {
        OVREyeGaze[] eyeGazeComponents = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None);
        foreach (OVREyeGaze eyeGaze in eyeGazeComponents)
        {
            if (eyeGaze.gameObject.name.Contains(eyeName))
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

        // Create and configure the LineRenderer for the correct eyes
        // Create and configure the LineRenderer for the left eye.
        GameObject calLeftLineObject = new GameObject("CalibratedLeftGazeLine");
        calLeftLineRenderer = calLeftLineObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(calLeftLineRenderer, Color.blue);
        calLeftLineRenderer.transform.parent = transform;
        // Create and configure the LineRenderer for the right eye.
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
        Material lineMaterial = new Material(Shader.Find("Unlit/Color"));
        lineMaterial.color = color;
        lineRenderer.material = lineMaterial;
        //only render in the editor
        lineRenderer.gameObject.layer = LayerMask.NameToLayer("Gaze LineRenders");
    }

    private void CreateConvergenceMarker()
    {
        if (convergenceMarkerPrefab == null)
        {
            Debug.LogError("Convergence Marker Prefab not assigned!");
            return;
        }

        convergenceMarker = Instantiate(convergenceMarkerPrefab);
        convergenceMarker.name = "ConvergenceMarker";
        //only render in the editor
        convergenceMarker.layer = LayerMask.NameToLayer("Gaze LineRenders");
        convergenceMarker.transform.parent = transform;
    }

    void Update()
    {
        // draw *calibrated* rays from manager’s transforms
        if (EyeCalibrationManager.Instance.Calibrated)
        {
            VisualizeGaze(EyeCalibrationManager.Instance.LeftGazeTransform,
                          calLeftLineRenderer);
            VisualizeGaze(EyeCalibrationManager.Instance.RightGazeTransform,
                          calRightLineRenderer);
        }

        // draw *raw* rays from OVREyeGaze
        VisualizeGaze(leftEyeGaze.transform, leftLineRenderer);
        VisualizeGaze(rightEyeGaze.transform, rightLineRenderer);

        VisualizeConvergence();
    }

    private void VisualizeGaze(Transform src, LineRenderer lr)
    {
        Ray r = new Ray(src.position, src.forward);
        lr.SetPosition(0, r.origin);
        lr.SetPosition(1, r.origin + r.direction * maxDistance);
    }

    private void VisualizeConvergence()
    {
        Transform leftSrc, rightSrc;

        if (EyeCalibrationManager.Instance.Calibrated)
        {
            // ----- use calibrated rays -----
            leftSrc = EyeCalibrationManager.Instance.LeftGazeTransform;
            rightSrc = EyeCalibrationManager.Instance.RightGazeTransform;
        }
        else
        {
            // ----- fall back to raw OVREyeGaze -----
            leftSrc = leftEyeGaze ? leftEyeGaze.transform : null;
            rightSrc = rightEyeGaze ? rightEyeGaze.transform : null;
        }

        // sanity check
        if (leftSrc == null || rightSrc == null)
        {
            if (convergenceMarker) convergenceMarker.SetActive(false);
            return;
        }

        convergenceMarker.SetActive(true);

        Vector3 vergencePoint = GetVergencePoint(
            leftSrc.position, leftSrc.forward.normalized,
            rightSrc.position, rightSrc.forward.normalized);

        convergenceMarker.transform.position = vergencePoint;
    }

    private Vector3 GetVergencePoint(Vector3 leftPos, Vector3 leftDir, Vector3 rightPos, Vector3 rightDir)
    {
        Vector3 w0 = leftPos - rightPos;
        float a = Vector3.Dot(leftDir, leftDir);
        float b = Vector3.Dot(leftDir, rightDir);
        float c = Vector3.Dot(rightDir, rightDir);
        float d = Vector3.Dot(leftDir, w0);
        float e = Vector3.Dot(rightDir, w0);
        float denominator = (a * c) - (b * b);

        if (Mathf.Abs(denominator) < 1e-6f)
        {
            // Lines are nearly parallel; return the midpoint of the closest points.
            float t = d / a;
            float s = e / c;
            Vector3 pointOnLeft = leftPos + t * leftDir;
            Vector3 pointOnRight = rightPos + s * rightDir;
            return (pointOnLeft + pointOnRight) * 0.5f;
        }

        // Renamed variables to avoid conflicts.
        float tNonParallel = (b * e - c * d) / denominator;
        float sNonParallel = (a * e - b * d) / denominator;

        // Clamp t and s to reasonable ranges. Adjust these values as needed.
        tNonParallel = Mathf.Clamp(tNonParallel, 0f, 15f);
        sNonParallel = Mathf.Clamp(sNonParallel, 0f, 15f);

        Vector3 pointOnLeftNonParallel = leftPos + tNonParallel * leftDir;
        Vector3 pointOnRightNonParallel = rightPos + sNonParallel * rightDir;
        return (pointOnLeftNonParallel + pointOnRightNonParallel) * 0.5f;
    }
}
