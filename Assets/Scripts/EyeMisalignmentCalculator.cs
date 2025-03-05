using System.Collections.Generic;
using UnityEngine;

public class EyeMisalignmentCalculator
{
    /// <summary>
    /// Calculates the misalignment between two eyes' gaze directions, representing a potential binocular vision dysfunction.
    /// </summary>
    /// <param name="leftEyePos">The position of the left eye.</param>
    /// <param name="leftGazeDir">The gaze direction vector of the left eye (normalized).</param>
    /// <param name="rightEyePos">The position of the right eye.</param>
    /// <param name="rightGazeDir">The gaze direction vector of the right eye (normalized).</param>
    /// <param name="targetCenter">The center point of the fixation target.</param>
    /// <param name="misalignmentData">Output parameter to store detailed misalignment information.</param>
    /// <returns>A MisalignmentResult object containing overall misalignment metrics.</returns>
    public static MisalignmentResult CalculateEyeMisalignment(Vector3 leftEyePos, Vector3 leftGazeDir, Vector3 rightEyePos, Vector3 rightGazeDir, Vector3 targetCenter, out MisalignmentData misalignmentData)
    {
        // Initialize misalignment data object
        misalignmentData = new MisalignmentData();

        // Calculate the vergence point (where the eyes are attempting to converge)
        Vector3 vergencePoint = GetVergencePoint(leftEyePos, leftGazeDir, rightEyePos, rightGazeDir);
        misalignmentData.VergencePoint = vergencePoint;

        // Calculate the distance from each eye to the target.
        float leftTargetDistance = Vector3.Distance(targetCenter, leftEyePos);
        float rightTargetDistance = Vector3.Distance(targetCenter, rightEyePos);
        //check for bad data.
        if (leftTargetDistance < 0.05f || rightTargetDistance < 0.05f)
        {
            Debug.LogWarning("Target too close to eye, returning null.");
            return null;
        }

        // Calculate the convergence distance (distance from the midpoint between the eyes to the vergence point).
        Vector3 eyeMidpoint = (leftEyePos + rightEyePos) * 0.5f;
        float convergenceDistance = Vector3.Distance(vergencePoint, eyeMidpoint); // Use vergence point for convergence distance
        //check for bad data.
        if (convergenceDistance < 0.05f)
        {
            Debug.LogWarning("Convergence point too close, returning null.");
            return null;
        }

        // Calculate the angular error for each eye.
        float leftAngularError = CalculateAngularError(leftEyePos, leftGazeDir, targetCenter);
        float rightAngularError = CalculateAngularError(rightEyePos, rightGazeDir, targetCenter);
        misalignmentData.LeftAngularError = leftAngularError;
        misalignmentData.RightAngularError = rightAngularError;

        // Convert the angular errors to phoria in prism diopters.
        float leftPhoria = ConvertRadiansToPrismDiopters(leftAngularError);
        float rightPhoria = ConvertRadiansToPrismDiopters(rightAngularError);
        misalignmentData.LeftPhoria = leftPhoria;
        misalignmentData.RightPhoria = rightPhoria;

        // Calculate Tropia
        float leftTropia = CalculateTropia(leftEyePos, leftGazeDir, targetCenter);
        float rightTropia = CalculateTropia(rightEyePos, rightGazeDir, targetCenter);
        misalignmentData.LeftTropia = leftTropia;
        misalignmentData.RightTropia = rightTropia;

        // Current default calculation
        //Phoria is not calculated by convergence values.
        float overallPhoria = 0;
        misalignmentData.OverallPhoria = overallPhoria;

        float overallTropia = (leftTropia + rightTropia) / 2f;
        misalignmentData.OverallTropia = overallTropia;

        // Determine if there is a significant misalignment.
        bool significantMisalignment = Mathf.Abs(overallPhoria) > 1.0f || Mathf.Abs(leftPhoria) > 1f || Mathf.Abs(rightPhoria) > 1f || Mathf.Abs(overallTropia) > 1f || Mathf.Abs(leftTropia) > 1f || Mathf.Abs(rightTropia) > 1f;
        misalignmentData.SignificantMisalignment = significantMisalignment;

        // Determine the type of misalignment (exo, eso, hyper, hypo).
        MisalignmentType type = DetermineMisalignmentType(leftPhoria, rightPhoria, leftTropia, rightTropia);
        misalignmentData.MisalignmentType = type;

        return new MisalignmentResult(overallPhoria, leftPhoria, rightPhoria, overallTropia, leftTropia, rightTropia, significantMisalignment, type);
    }

    /// <summary>
    /// Represents the type of eye misalignment.
    /// </summary>
    public enum MisalignmentType
    {
        None,
        Exophoria, // Eyes turn outward.
        Esophoria, // Eyes turn inward.
        Hyperphoria, // One eye turns upward.
        Hypophoria, // One eye turns downward.
        Exotropia, // Eyes turn outward (manifest).
        Esotropia, // Eyes turn inward (manifest).
        Hypertropia, // One eye turns upward (manifest).
        Hypotropia, // One eye turns downward (manifest).
        Mixed // Combination of misalignment types.
    }

    /// <summary>
    /// Data struct to hold misalignment info
    /// </summary>
    public class MisalignmentData
    {
        public Vector3 VergencePoint;
        public float LeftAngularError;
        public float RightAngularError;
        public float LeftPhoria;
        public float RightPhoria;
        public float OverallPhoria;
        public float LeftTropia;
        public float RightTropia;
        public float OverallTropia;
        public bool SignificantMisalignment;
        public MisalignmentType MisalignmentType;
    }
    /// <summary>
    /// Structure to represent the result of the eye misalignment calculation.
    /// </summary>
    public class MisalignmentResult
    {
        public float OverallPhoria { get; private set; }
        public float LeftPhoria { get; private set; }
        public float RightPhoria { get; private set; }

        public float OverallTropia { get; private set; }
        public float LeftTropia { get; private set; }
        public float RightTropia { get; private set; }

        public bool SignificantMisalignment { get; private set; }
        public MisalignmentType MisalignmentType { get; private set; }

        public MisalignmentResult(float overallPhoria, float leftPhoria, float rightPhoria, float overallTropia, float leftTropia, float rightTropia, bool significantMisalignment, MisalignmentType misalignmentType)
        {
            OverallPhoria = overallPhoria;
            LeftPhoria = leftPhoria;
            RightPhoria = rightPhoria;
            OverallTropia = overallTropia;
            LeftTropia = leftTropia;
            RightTropia = rightTropia;
            SignificantMisalignment = significantMisalignment;
            MisalignmentType = misalignmentType;
        }
    }

    /// <summary>
    /// Determines the type of misalignment based on the individual eye phorias and tropias.
    /// </summary>
    /// <param name="leftPhoria">The phoria of the left eye.</param>
    /// <param name="rightPhoria">The phoria of the right eye.</param>
    /// <param name="leftTropia">The tropia of the left eye.</param>
    /// <param name="rightTropia">The tropia of the right eye.</param>
    /// <returns>The MisalignmentType.</returns>
    private static MisalignmentType DetermineMisalignmentType(float leftPhoria, float rightPhoria, float leftTropia, float rightTropia)
    {
        bool isExo = leftPhoria > 0 && rightPhoria > 0;
        bool isEso = leftPhoria < 0 && rightPhoria < 0;
        bool isHyper = leftPhoria > 0 && rightPhoria < 0;
        bool isHypo = leftPhoria < 0 && rightPhoria > 0;

        bool isExotropia = leftTropia > 0 && rightTropia > 0;
        bool isEsotropia = leftTropia < 0 && rightTropia < 0;
        bool isHypertropia = leftTropia > 0 && rightTropia < 0;
        bool isHypotropia = leftTropia < 0 && rightTropia > 0;

        if (isExotropia) return MisalignmentType.Exotropia;
        if (isEsotropia) return MisalignmentType.Esotropia;
        if (isHypertropia) return MisalignmentType.Hypertropia;
        if (isHypotropia) return MisalignmentType.Hypotropia;

        if (isExo && isHyper)
        {
            return MisalignmentType.Mixed;
        }
        else if (isExo && isHypo)
        {
            return MisalignmentType.Mixed;
        }
        else if (isEso && isHyper)
        {
            return MisalignmentType.Mixed;
        }
        else if (isEso && isHypo)
        {
            return MisalignmentType.Mixed;
        }
        else if (isExo)
        {
            return MisalignmentType.Exophoria;
        }
        else if (isEso)
        {
            return MisalignmentType.Esophoria;
        }
        else if (isHyper)
        {
            return MisalignmentType.Hyperphoria;
        }
        else if (isHypo)
        {
            return MisalignmentType.Hypophoria;
        }
        else
        {
            return MisalignmentType.None;
        }
    }

    /// <summary>
    /// Calculates the angular error between the eye's gaze direction and the direction to the target.
    /// </summary>
    /// <param name="eyePos">The position of the eye.</param>
    /// <param name="eyeDir">The gaze direction vector of the eye (normalized).</param>
    /// <param name="targetCenter">The center point of the target.</param>
    /// <returns>The angular error in radians.</returns>
    private static float CalculateAngularError(Vector3 eyePos, Vector3 eyeDir, Vector3 targetCenter)
    {
        Vector3 targetDirection = (targetCenter - eyePos).normalized;
        float dotProduct = Vector3.Dot(eyeDir, targetDirection);
        //Clamp the dot product to account for floating point errors.
        dotProduct = Mathf.Clamp(dotProduct, -1.0f, 1.0f);
        return Mathf.Acos(dotProduct); // Angle in radians
    }

    /// <summary>
    /// Calculates the tropia for a single eye.
    /// </summary>
    /// <param name="eyePos">The position of the eye.</param>
    /// <param name="eyeDir">The gaze direction vector of the eye (normalized).</param>
    /// <param name="targetCenter">The center point of the target.</param>
    /// <returns>The tropia in prism diopters.</returns>
    private static float CalculateTropia(Vector3 eyePos, Vector3 eyeDir, Vector3 targetCenter)
    {
        // Tropia is the manifest deviation. If the eye is looking directly at the target, there's no tropia.
        Vector3 targetDirection = (targetCenter - eyePos).normalized;
        float angle = Vector3.Angle(eyeDir, targetDirection);
        float radians = angle * Mathf.Deg2Rad;
        return ConvertRadiansToPrismDiopters(radians);
        // If the angle is positive, it's an outward deviation (exo).
        // If the angle is negative, it's an inward deviation (eso).

    }

    /// <summary>
    /// Converts an angle in radians to prism diopters.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The equivalent value in prism diopters.</returns>
    private static float ConvertRadiansToPrismDiopters(float radians)
    {
        // Prism diopters are approximately equal to the tangent of the angle in radians.
        return Mathf.Tan(radians);
    }

    /// <summary>
    /// Computes the vergence point (where the two gaze rays intersect or come closest).
    /// </summary>
    /// <param name="leftPos">The position of the left eye.</param>
    /// <param name="leftDir">The gaze direction vector of the left eye (normalized).</param>
    /// <param name="rightPos">The position of the right eye.</param>
    /// <param name="rightDir">The gaze direction vector of the right eye (normalized).</param>
    /// <returns>The vergence point in world space.</returns>
    private static Vector3 GetVergencePoint(Vector3 leftPos, Vector3 leftDir, Vector3 rightPos, Vector3 rightDir)
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
