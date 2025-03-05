using UnityEngine;

public class VisionTargetSpaceTransform : MonoBehaviour
{
    [Tooltip("Distance in front of the player to position the vision target.")]
    public float targetDistance = 2.0f;

    [Tooltip("Run continuously to update the target position every frame.")]
    [SerializeField]
    private bool updateContinuously = false;

    private Transform centerEyeAnchor;

    void Start()
    {
        // Find the CenterEyeAnchor within the OVRCameraRig.  This is the recommended way to
        // get the user's head position.  We do this in Start() to ensure the OVRCameraRig
        // has been initialized.
        OVRCameraRig cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            centerEyeAnchor = cameraRig.centerEyeAnchor;
            if (centerEyeAnchor == null)
            {
                Debug.LogError("CenterEyeAnchor not found in OVRCameraRig!");
            }
        }
        else
        {
            Debug.LogError("OVRCameraRig not found in the scene!");
        }

        // Initial positioning of the target.
        PositionTarget();
    }

    void Update()
    {
        // Update the target position every frame to follow the player.
        if (updateContinuously)
        {
            PositionTarget();
        }
    }

    private void PositionTarget()
    {
        if (centerEyeAnchor == null)
        {
            return; // Don't do anything if we don't have the camera reference.
        }

        // Calculate the target position in front of the camera.
        Vector3 targetPosition = centerEyeAnchor.position + centerEyeAnchor.forward * targetDistance;

        // Set the position of this GameObject (the vision target).
        transform.position = targetPosition;
    }
}
