using System;
using System.Buffers.Binary;
using System.IO;
using UnityEngine;

public class EyeGazeLoggerLite : MonoBehaviour
{
    [Header("OVR Eye refs")]
    public OVREyeGaze leftEye;
    public OVREyeGaze rightEye;
    [Tooltip("Hz; 0 = every FixedUpdate")] public float sampleRate = 60f;

    private FileStream fs;

    // 4 bytes in a float
    // 3 floats per vec3 (x y z)
    // n = number of vec3 being logged
    // 4 bytes for the timestamp
    private static readonly int n = 8;
    private byte[] scratch = new byte[4 * 3 * n + 4];
    private float nextTick;

    void Start()
    {
        string path = Path.Combine(Application.persistentDataPath,
                      $"gaze_{DateTime.Now:yyyyMMdd_HHmmss}.bin");

        fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                            FileShare.None, 4096, FileOptions.WriteThrough);

        // write tiny header: magic + version + Hz
        fs.Write(System.Text.Encoding.ASCII.GetBytes("GZB1"));  // 4B
        int hz = Mathf.RoundToInt(sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(scratch.AsSpan(0, 4), hz);
        fs.Write(scratch, 0, 4);

        nextTick = Time.time;
    }

    void FixedUpdate()
    {
        if (sampleRate > 0 && Time.time < nextTick) return;
        nextTick += 1f / sampleRate;

        if (!leftEye || !rightEye || !leftEye.EyeTrackingEnabled) return;

        int o = 0;
        o = WriteFloat(Time.time, scratch, o);
        o = WriteVec3(leftEye.transform.position, scratch, o);
        o = WriteVec3(leftEye.transform.forward, scratch, o);
        o = WriteVec3(rightEye.transform.position, scratch, o);
        o = WriteVec3(rightEye.transform.forward, scratch, o);

        if (EyeCalibrationManager.Instance.Calibrated)
        {
            o = WriteVec3(EyeCalibrationManager.Instance.LeftGazeTransform.position, scratch, o);
            o = WriteVec3(EyeCalibrationManager.Instance.LeftGazeTransform.forward, scratch, o);
            o = WriteVec3(EyeCalibrationManager.Instance.RightGazeTransform.position, scratch, o);
            o = WriteVec3(EyeCalibrationManager.Instance.RightGazeTransform.forward, scratch, o);
        }
        else
        {
            o = WriteVec3(Vector3.zero, scratch, o);
            o = WriteVec3(Vector3.zero, scratch, o);
            o = WriteVec3(Vector3.zero, scratch, o);
            o = WriteVec3(Vector3.zero, scratch, o);
        }

        fs.Write(scratch, 0, o); // 4+24+24 bytes = 52 per sample
    }

    private static int WriteFloat(float f, byte[] buf, int offset)
    {
        // using System.Buffers.Binary;
        int bits = BitConverter.SingleToInt32Bits(f);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(offset, 4), bits);

        return offset + 4;
    }
    private static int WriteVec3(Vector3 v, byte[] buf, int offset)
    {
        offset = WriteFloat(v.x, buf, offset);
        offset = WriteFloat(v.y, buf, offset);
        offset = WriteFloat(v.z, buf, offset);
        return offset;
    }

    void OnDestroy()
    {
        fs?.Flush();
        fs?.Dispose();
    }
}
