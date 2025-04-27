using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// EyeCalibrationManager — binocular calibration that now **drives live Transforms**
/// representing each calibrated gaze ray. Position = eye origin, forward = corrected
/// gaze direction.  Use these Transforms directly in your diagnostics (raycasts, UX).
/// </summary>
public class EyeCalibrationManager : MonoBehaviour
{
    // ───────── CONFIG ─────────
    private const float MinSepDeg = 4f;
    public static EyeCalibrationManager Instance { get; private set; }

    [Header("Random target generation (deg)")]
    [SerializeField] private int targetCount = 12;
    [SerializeField] private float minAzDeg = -15f;
    [SerializeField] private float maxAzDeg = 15f;
    [SerializeField] private float minElDeg = -10f;
    [SerializeField] private float maxElDeg = 10f;

    [Header("Depth (m)")]
    [SerializeField] private float minDistanceM = 1.5f;
    [SerializeField] private float maxDistanceM = 1.5f;

    [SerializeField] private GameObject targetPrefab;

    [Header("Sampling")]
    [SerializeField] private int samplesPerTarget = 60;
    [SerializeField] private KeyCode debugAdvanceKey = KeyCode.Space;

    [Header("Eye sources")]
    [SerializeField] private OVREyeGaze leftEye;
    [SerializeField] private OVREyeGaze rightEye;

    [Header("Output gaze transforms")]
    [Tooltip("Optional placeholders. If left null, manager creates them at runtime.")]
    [SerializeField] private Transform leftGazeTransform;
    [SerializeField] private Transform rightGazeTransform;

    // ───────── OUTPUT (vectors still exposed) ─────────
    public bool Calibrated { get; private set; }
    public Vector3 LeftDirection { get; private set; }
    public Vector3 RightDirection { get; private set; }
    public Transform LeftGazeTransform  => leftGazeTransform;
    public Transform RightGazeTransform => rightGazeTransform;
    public System.Action OnCalibrated;

    // ───────── STATE ─────────
    private readonly List<Transform> dots = new();
    private readonly List<Vector2> rawLeft = new();
    private readonly List<Vector2> rawRight = new();
    private readonly List<Vector2> targets = new();

    private int idx = -1, sampleHere = 0;
    private Matrix2x2 AL = Matrix2x2.identity, AR = Matrix2x2.identity;
    private Vector2 bL = Vector2.zero, bR = Vector2.zero;
    private Transform head;

    // ───────── UNITY ─────────
    private void Awake()
    {
        Instance = this;
        head = Camera.main.transform;
        if (!targetPrefab) { Debug.LogError("Assign targetPrefab"); enabled = false; return; }
        if (!leftEye) leftEye = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.Contains("Left"));
        if (!rightEye) rightEye = FindObjectsByType<OVREyeGaze>(FindObjectsSortMode.None).FirstOrDefault(e => e.name.Contains("Right"));
        if (!leftGazeTransform) leftGazeTransform = new GameObject("CalibratedLeftGaze").transform;
        if (!rightGazeTransform) rightGazeTransform = new GameObject("CalibratedRightGaze").transform;
        SpawnDots();
    }

    private void Update()
    {
        if (leftEye == null || rightEye == null || !leftEye.EyeTrackingEnabled || !rightEye.EyeTrackingEnabled) return;

        Vector2 rawL = ToYawPitch(leftEye.transform.forward);
        Vector2 rawR = ToYawPitch(rightEye.transform.forward);

        LeftDirection = FromYawPitch((AL * rawL) + bL);
        RightDirection = FromYawPitch((AR * rawR) + bR);

        // Drive output transforms
        leftGazeTransform.position = leftEye.transform.position;
        leftGazeTransform.rotation = Quaternion.LookRotation(LeftDirection, Vector3.up);
        rightGazeTransform.position = rightEye.transform.position;
        rightGazeTransform.rotation = Quaternion.LookRotation(RightDirection, Vector3.up);

        if (idx >= 0 && idx < dots.Count)
        {
            rawLeft.Add(rawL);
            rawRight.Add(rawR);
            targets.Add(ToYawPitch((dots[idx].position - leftEye.transform.position).normalized));
            if (++sampleHere >= samplesPerTarget || Input.GetKeyDown(debugAdvanceKey)) Advance();
        }
    }

    // ───────── CALIBRATION FLOW ─────────
    public void BeginCalibration()
    {
        Calibrated = false;
        idx = 0; sampleHere = 0;
        rawLeft.Clear(); rawRight.Clear(); targets.Clear();
        ShowDot(idx);
    }

    private void Advance()
    {
        idx++; sampleHere = 0;
        if (idx < dots.Count) ShowDot(idx); else Solve();
    }

    // ───────── TARGET SPAWN ─────────
    private void SpawnDots()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        for (int i = 0; i < targetCount; i++)
        {
            Vector2 ang;
            int guard = 0;
            do
            {
                ang = new Vector2(Random.Range(minAzDeg, maxAzDeg), Random.Range(minElDeg, maxElDeg));
            } while (!IsFarEnough(ang) && ++guard < 50);

            float d = Random.Range(minDistanceM, maxDistanceM);
            Vector3 dir = FromYawPitchDeg(ang);
            Vector3 pos = head.position + dir * d;
            var dot = Instantiate(targetPrefab, pos, Quaternion.LookRotation(-dir), transform);
            dot.SetActive(false);
            dots.Add(dot.transform);
        }
    }

    private bool IsFarEnough(Vector2 cand)
    {
        foreach (var t in dots)
        {
            Vector2 other = ToYawPitchDeg((t.position - head.position).normalized);
            if (Vector2.Distance(cand, other) < MinSepDeg) return false;
        }
        return true;
    }

    private void ShowDot(int i)
    {
        for (int j = 0; j < dots.Count; j++) dots[j].gameObject.SetActive(j == i);
    }

    // ───────── SOLVER ─────────
    private void Solve()
    {
        (AL, bL) = SolveAffine(rawLeft, targets);
        (AR, bR) = SolveAffine(rawRight, targets);
        Calibrated = true;
        Debug.Log("Binocular calibration complete — transforms live");
        OnCalibrated?.Invoke();
        ShowDot(-1);
    }

    private (Matrix2x2, Vector2) SolveAffine(List<Vector2> raw, List<Vector2> tgt)
    {
        int n = raw.Count; if (n < 4) return (Matrix2x2.identity, Vector2.zero);
        double sx = 0, sy = 0, tx = 0, ty = 0, sxx = 0, syy = 0, sxy = 0, tmx = 0, tmy = 0, tnx = 0, tny = 0;
        for (int i = 0; i < n; i++)
        {
            Vector2 m = raw[i]; Vector2 t = tgt[i];
            sx += m.x; sy += m.y; tx += t.x; ty += t.y;
            sxx += m.x * m.x; syy += m.y * m.y; sxy += m.x * m.y;
            tmx += t.x * m.x; tmy += t.y * m.x; tnx += t.x * m.y; tny += t.y * m.y;
        }
        double det = sxx * syy - sxy * sxy; if (Mathf.Abs((float)det) < 1e-7f) return (Matrix2x2.identity, Vector2.zero);
        Matrix2x2 Afit = new Matrix2x2(
            (float)((tmx * syy - tnx * sxy) / det),
            (float)((tnx * sxx - tmx * sxy) / det),
            (float)((tmy * syy - tny * sxy) / det),
            (float)((tny * sxx - tmy * sxy) / det));
        Vector2 meanM = new Vector2((float)(sx / n), (float)(sy / n));
        Vector2 meanT = new Vector2((float)(tx / n), (float)(ty / n));
        Vector2 bfit = meanT - (Afit * meanM);
        return (Afit, bfit);
    }

    // ───────── MATH HELPERS ─────────
    private static Vector2 ToYawPitch(Vector3 dir) { dir.Normalize(); return new(Mathf.Atan2(dir.x, dir.z), Mathf.Asin(dir.y)); }
    private static Vector2 ToYawPitchDeg(Vector3 dir) => ToYawPitch(dir) * Mathf.Rad2Deg;
    private static Vector3 FromYawPitch(Vector2 yp) { float c = Mathf.Cos(yp.y); return new(Mathf.Sin(yp.x) * c, Mathf.Sin(yp.y), Mathf.Cos(yp.x) * c); }
    private static Vector3 FromYawPitchDeg(Vector2 deg) => FromYawPitch(deg * Mathf.Deg2Rad);

    private struct Matrix2x2
    {
        public float m00, m01, m10, m11;
        public Matrix2x2(float m00, float m01, float m10, float m11)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m10 = m10;
            this.m11 = m11;
        }
        public static Matrix2x2 identity => new Matrix2x2(1, 0, 0, 1);
        public static Vector2 operator *(Matrix2x2 M, Vector2 v)
        {
            return new Vector2(M.m00 * v.x + M.m01 * v.y,
                               M.m10 * v.x + M.m11 * v.y);
        }
    }
}
