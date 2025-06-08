using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class StimulusController : MonoBehaviour
{
    [SerializeField] private GameObject leftEyeQuad;
    [SerializeField] private GameObject rightEyeQuad;
    [SerializeField] private Material crossMaterial;
    [SerializeField] private Material blankMaterial;
    [SerializeField] private Material bigRingMaterial;
    [SerializeField] private Material dotMaterial;
    [SerializeField] private bool dominantIsLeft = true;
    [SerializeField] private float fieldOfViewDegrees = 90f; // Default FOV
    
    private Vector2 currentOffsetDeg = Vector2.zero;
    private float pixelsPerDeg;
    
    public Vector2 CurrentOffsetDeg => currentOffsetDeg;
    
    private void Start()
    {
        // Calculate pixelsPerDeg based on XR display parameters
        // Get texture width without modifying the readonly struct
        int textureWidth = XRSettings.eyeTextureWidth > 0 ? XRSettings.eyeTextureWidth : Screen.width;
        
        // Use the provided field of view or estimate it
        float horizontalFOV = fieldOfViewDegrees;
        
        // Calculate pixels per degree
        pixelsPerDeg = textureWidth / horizontalFOV;
        
        // Ensure quads are on correct layers
        if (leftEyeQuad != null)
        {
            leftEyeQuad.layer = LayerMask.NameToLayer("LeftOnly");
        }
        
        if (rightEyeQuad != null)
        {
            rightEyeQuad.layer = LayerMask.NameToLayer("RightOnly");
        }
        
        // Start with blank
        ShowBlankBothEyes();
    }
    
    public void ShowCentralCrossBothEyes()
    {
        if (leftEyeQuad != null && rightEyeQuad != null && crossMaterial != null)
        {
            SetMaterial(leftEyeQuad, crossMaterial);
            SetMaterial(rightEyeQuad, crossMaterial);
            SetQuadScale(leftEyeQuad, new Vector3(0.05f, 0.05f, 0.001f));
            SetQuadScale(rightEyeQuad, new Vector3(0.05f, 0.05f, 0.001f));
            ApplyOffsetDeg(Vector2.zero); // Reset offset
        }
    }
    
    public void ShowCrossDominantEye_BlankOther()
    {
        if (leftEyeQuad != null && rightEyeQuad != null && crossMaterial != null && blankMaterial != null)
        {
            if (dominantIsLeft)
            {
                SetMaterial(leftEyeQuad, crossMaterial);
                SetMaterial(rightEyeQuad, blankMaterial);
            }
            else
            {
                SetMaterial(leftEyeQuad, blankMaterial);
                SetMaterial(rightEyeQuad, crossMaterial);
            }
            
            SetQuadScale(leftEyeQuad, new Vector3(0.05f, 0.05f, 0.001f));
            SetQuadScale(rightEyeQuad, new Vector3(0.05f, 0.05f, 0.001f));
        }
    }
    
    public void ShowBigRingBothEyes()
    {
        if (leftEyeQuad != null && rightEyeQuad != null && bigRingMaterial != null)
        {
            SetMaterial(leftEyeQuad, bigRingMaterial);
            SetMaterial(rightEyeQuad, bigRingMaterial);
            SetQuadScale(leftEyeQuad, new Vector3(0.2f, 0.2f, 0.001f));
            SetQuadScale(rightEyeQuad, new Vector3(0.2f, 0.2f, 0.001f));
        }
    }
    
    public void ShowDotLeftEye()
    {
        if (leftEyeQuad != null && dotMaterial != null)
        {
            SetMaterial(leftEyeQuad, dotMaterial);
            SetQuadScale(leftEyeQuad, new Vector3(0.01f, 0.01f, 0.001f));
        }
    }
    
    public void ShowDotRightEye()
    {
        if (rightEyeQuad != null && dotMaterial != null)
        {
            SetMaterial(rightEyeQuad, dotMaterial);
            SetQuadScale(rightEyeQuad, new Vector3(0.01f, 0.01f, 0.001f));
        }
    }
    
    public void ShowBlankBothEyes()
    {
        if (leftEyeQuad != null && rightEyeQuad != null && blankMaterial != null)
        {
            SetMaterial(leftEyeQuad, blankMaterial);
            SetMaterial(rightEyeQuad, blankMaterial);
        }
    }
    
    public void ApplyOffsetDeg(Vector2 offsetDeg)
    {
        currentOffsetDeg = offsetDeg;
        
        // Convert degrees to pixels/unity units
        Vector2 offsetUnits = DegreesToUnits(offsetDeg);
        
        // Apply in opposite directions to each eye
        if (leftEyeQuad != null && rightEyeQuad != null)
        {
            // Left eye gets negative of half the offset
            leftEyeQuad.transform.localPosition = new Vector3(
                -offsetUnits.x / 2f,
                -offsetUnits.y / 2f,
                leftEyeQuad.transform.localPosition.z
            );
            
            // Right eye gets positive of half the offset
            rightEyeQuad.transform.localPosition = new Vector3(
                offsetUnits.x / 2f,
                offsetUnits.y / 2f,
                rightEyeQuad.transform.localPosition.z
            );
        }
    }
    
    private Vector2 DegreesToUnits(Vector2 degrees)
    {
        // Convert degrees to unity units based on distance and FOV
        // For small angles, tan(angle) ≈ angle in radians
        float distance = GetComponentInParent<Camera>()?.nearClipPlane ?? 0.1f;
        
        return new Vector2(
            distance * Mathf.Tan(degrees.x * Mathf.Deg2Rad),
            distance * Mathf.Tan(degrees.y * Mathf.Deg2Rad)
        );
    }
    
    private void SetMaterial(GameObject obj, Material mat)
    {
        if (obj != null && mat != null)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = mat;
            }
        }
    }
    
    private void SetQuadScale(GameObject obj, Vector3 scale)
    {
        if (obj != null)
        {
            obj.transform.localScale = scale;
        }
    }
} 