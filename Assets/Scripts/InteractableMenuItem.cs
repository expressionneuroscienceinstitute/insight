using UnityEngine;
using UnityEngine.Events; // Required for UnityEvents
using UnityEngine.UI; // Required for Image

// Defines an item that can be interacted with via gaze.
public class InteractableMenuItem : MonoBehaviour
{
    [Tooltip("Event triggered when the user gazes at this item for the required dwell time.")]
    public UnityEvent OnGazeSelect = new UnityEvent();

    [Tooltip("Is this item currently active and interactable?")]
    public bool IsActive = true; 

    [Header("Dwell Visuals")]
    [Tooltip("Optional UI Image element to show dwell progress radially.")]
    public Image dwellProgressImage; // Changed from Slider to Image

    void Awake()
    {
        // Ensure progress image is hidden initially
        if (dwellProgressImage != null)
        {
            dwellProgressImage.fillAmount = 0f;
            dwellProgressImage.gameObject.SetActive(false);
        }
    }

    // Called by the GazePointerController when interaction is confirmed.
    public void Select()
    {
        if (IsActive)
        {
            Debug.Log($"Interactable selected: {gameObject.name}");
            UpdateDwellProgress(0f); // Reset progress image on select
            OnGazeSelect.Invoke();
        }
    }

    // Called by GazePointerController to update the loading bar visual
    public void UpdateDwellProgress(float progress)
    {
        if (dwellProgressImage != null)
        {
            // Use fillAmount for radial progress (progress should be 0 to 1)
            dwellProgressImage.fillAmount = progress;
            // Show only when dwelling and active (and progress > 0 to avoid flash)
            dwellProgressImage.gameObject.SetActive(progress > 0 && IsActive); 
        }
    }

    // Optional: Method to activate/deactivate the item
    public void SetActive(bool active)
    {
        IsActive = active;
        // Optional: Add visual feedback for active/inactive state (e.g., change color)
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Keep existing color logic or adjust as needed
             renderer.material.color = IsActive ? Color.white : Color.gray; 
        }
        // Ensure progress image is hidden if deactivated
        if (!IsActive)
        {
            UpdateDwellProgress(0f);
        }
    }
} 