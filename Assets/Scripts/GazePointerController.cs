using UnityEngine;

public class GazePointerController : MonoBehaviour
{
    [Tooltip("Maximum distance for the gaze raycast.")]
    public float maxDistance = 10f;

    [Tooltip("Time in seconds the user must gaze at an interactable to select it.")]
    public float dwellTime = 1.5f;

    private Transform cameraTransform;
    private InteractableMenuItem currentInteractable = null;
    private float gazeTimer = 0f;

    // Optional: Visual feedback for gaze point (e.g., a small sphere or reticle)
    [Tooltip("Optional visual indicator for the gaze point.")]
    public Transform gazeReticle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the main camera
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("GazePointerController: Main Camera not found. Please ensure your main camera is tagged 'MainCamera'.");
            enabled = false; // Disable script if no camera
            return;
        }

        if (gazeReticle)
        {
            gazeReticle.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraTransform == null) return;

        Ray gazeRay = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        bool hitInteractable = false;
        Vector3 hitPoint = cameraTransform.position + cameraTransform.forward * maxDistance; // Default point if nothing hit

        if (Physics.Raycast(gazeRay, out hit, maxDistance))
        {
            hitPoint = hit.point;
            // Try to get the InteractableMenuItem component directly from the hit collider
            InteractableMenuItem interactable = hit.collider.GetComponent<InteractableMenuItem>();
            
            // Check if the component exists and is active
            if (interactable != null && interactable.IsActive)
            {
                hitInteractable = true;
                if (currentInteractable == interactable)
                {
                    // Still looking at the same active interactable, increment timer
                    gazeTimer += Time.deltaTime;
                    
                    // Update dwell progress visual
                    currentInteractable.UpdateDwellProgress(gazeTimer / dwellTime);

                    if (gazeTimer >= dwellTime)
                    {
                        // Dwell time reached, trigger selection and reset
                        interactable.Select();
                        ResetGaze();
                    }
                }
                else
                {
                    // Switched to a new interactable, reset timer
                    ResetGaze();
                    currentInteractable = interactable;
                }
            }
        }

        // If not hitting an active interactable, reset the gaze timer and target
        if (!hitInteractable)
        {
            ResetGaze();
        }

        // Update reticle position and state
        if (gazeReticle)
        {
            gazeReticle.position = hitPoint;
            // Optional: Change reticle appearance based on hitting interactable/dwell progress
        }
    }

    private void ResetGaze()
    {
        if (currentInteractable != null)
        {
            // Reset the progress bar of the previous interactable
            currentInteractable.UpdateDwellProgress(0f);
            // Optional: Signal end of hover to the interactable if needed
            // currentInteractable.OnHoverEnd(); 
        }
        currentInteractable = null;
        gazeTimer = 0f;
    }
}
