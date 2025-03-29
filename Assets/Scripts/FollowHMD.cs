using Unity.VisualScripting;
using UnityEngine;

public class FollowHMD : MonoBehaviour
{
    [Tooltip("The camera to use as reference when positioning the TargetSpace.")]
    public Camera referenceCamera;

    [Tooltip("The offset in meters the TargetSpace's position should stay from the referenceCamera.")]
    public float positionOffset = 6f;

    [Tooltip("The speed at which TargetSpace should move towards the desired position.")]
    public float speed = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!referenceCamera)
        {
            Debug.LogError("ReferenceCamera was not defined, TargetSpace will not follow headset.");
        }
        else
        {
            Vector3 desiredPosition = referenceCamera.transform.position + referenceCamera.transform.rotation * referenceCamera.transform.forward * positionOffset;

            Vector3 directionToHMD = referenceCamera.transform.position - transform.position;

            // Ignore the vertical component to prevent tilting
            directionToHMD.y = 0;

            // If the direction is not zero, rotate to face the HMD
            if (directionToHMD != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToHMD);
            }

            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, speed * Time.deltaTime);
        }
    }
}
