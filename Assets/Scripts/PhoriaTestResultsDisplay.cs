using UnityEngine;
using TMPro;

public class PhoriaTestResultsDisplay : MonoBehaviour
{
    [Tooltip("Reference to the TextMeshPro - Text UI element for displaying test results.")]
    public TMP_Text resultsText;

    public void DisplayResults(string results)
    {
        if (resultsText != null)
        {
            resultsText.text = results;
        }
        else
        {
            Debug.LogError("Results Text UI element is not assigned.");
        }
    }
}