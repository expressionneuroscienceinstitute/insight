using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class EyeGazeReplay : MonoBehaviour
{
    [Tooltip("CSV file to replay (full path).")]
    public string csvPath;

    [Tooltip("Prefab holding a GazeLineRenderer. One gets cloned automatically.")]
    public GazeLineRenderer lineRendererPrefab;

    [Tooltip("Playback speed multiplier (1 = realtime).")]
    public float speed = 1f;

    private struct Frame
    {
        public float t;
        public Vector3 lPos, rPos;
        public Vector3 lDir, rDir;
    }

    private readonly List<Frame> frames = new();
    private float simClock;
    private int cursor;

    private GazeLineRenderer live;

    void Start()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError("Replay file not found: " + csvPath);
            enabled = false; return;
        }

        ParseCsv(csvPath);

        live = Instantiate(lineRendererPrefab);
        // tell it to ignore real eye-trackers
        live.leftEyeGaze = null;
        live.rightEyeGaze = null;
    }

    void Update()
    {
        if (frames.Count == 0) return;

        simClock += Time.deltaTime * speed;

        // advance cursor while next frame is in the past
        while (cursor + 1 < frames.Count && frames[cursor + 1].t <= simClock)
            cursor++;

        // simple no-interp playback
        Frame f = frames[cursor];

        live.VisualizeExternal(
            f.lPos, f.lDir,
            f.rPos, f.rDir);
    }

    private void ParseCsv(string path)
    {
        using var reader = new StreamReader(path);
        string header = reader.ReadLine();   // throw away header

        var inv = CultureInfo.InvariantCulture;
        while (!reader.EndOfStream)
        {
            string[] p = reader.ReadLine().Split(',');
            if (p.Length < 23) continue;     // sanity

            frames.Add(new Frame
            {
                t = float.Parse(p[0], inv),
                lPos = new Vector3(float.Parse(p[1], inv), float.Parse(p[2], inv), float.Parse(p[3], inv)),
                lDir = new Vector3(float.Parse(p[8], inv), float.Parse(p[9], inv), float.Parse(p[10], inv)),
                rPos = new Vector3(float.Parse(p[11], inv), float.Parse(p[12], inv), float.Parse(p[13], inv)),
                rDir = new Vector3(float.Parse(p[18], inv), float.Parse(p[19], inv), float.Parse(p[20], inv))
            });
        }
        Debug.Log($"Loaded {frames.Count} gaze frames");
    }
}
