using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR Menu Prefab Manager for creating and managing 3D VR menu interfaces
/// Designed for Meta Quest Pro with proper world-space positioning
/// </summary>
public class VRMenuPrefabManager : MonoBehaviour
{
    [Header("Menu Positioning")]
    [SerializeField] private float menuDistance = 2.0f;
    [SerializeField] private float menuHeight = 0.0f;
    [SerializeField] private Vector3 menuSize = new Vector3(3.0f, 2.0f, 0.1f);
    [SerializeField] private bool followPlayerRotation = false;
    
    [Header("Button Configuration")]
    [SerializeField] private Vector2 buttonSize = new Vector2(0.8f, 0.2f);
    [SerializeField] private float buttonSpacing = 0.25f;
    [SerializeField] private Color buttonNormalColor = new Color(0.2f, 0.3f, 0.8f, 0.8f);
    [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.5f, 1.0f, 0.9f);
    [SerializeField] private Color buttonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("Materials")]
    [SerializeField] private Material menuBackgroundMaterial;
    [SerializeField] private Material buttonMaterial;
    [SerializeField] private Material textMaterial;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerTransform;
    
    // UI Components
    private GameObject menuRoot;
    private Canvas worldCanvas;
    private VRMainMenu vrMainMenu;
    
    // Button GameObjects and components
    private GameObject[] menuButtons;
    private InteractableMenuItem[] buttonInteractables;
    private TextMeshProUGUI[] buttonTexts;
    
    void Start()
    {
        SetupPlayerReferences();
        CreateVRMenuInterface();
    }
    
    void Update()
    {
        UpdateMenuPosition();
    }
    
    private void SetupPlayerReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        if (playerTransform == null && playerCamera != null)
        {
            playerTransform = playerCamera.transform;
        }
    }
    
    private void CreateVRMenuInterface()
    {
        // Create root menu object
        menuRoot = new GameObject("VR_MainMenu_Root");
        menuRoot.transform.SetParent(this.transform);
        
        // Create world space canvas
        CreateWorldSpaceCanvas();
        
        // Create menu background
        CreateMenuBackground();
        
        // Create title
        CreateMenuTitle();
        
        // Create calibration status display
        CreateCalibrationStatusDisplay();
        
        // Create menu buttons
        CreateMenuButtons();
        
        // Add VRMainMenu component
        vrMainMenu = menuRoot.AddComponent<VRMainMenu>();
        ConfigureVRMainMenuComponent();
        
        // Position menu initially
        UpdateMenuPosition();
    }
    
    private void CreateWorldSpaceCanvas()
    {
        GameObject canvasObj = new GameObject("VR_Menu_Canvas");
        canvasObj.transform.SetParent(menuRoot.transform);
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = playerCamera;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Set canvas size and position
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(menuSize.x * 100, menuSize.y * 100); // Scale for world space
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * 0.01f; // Scale down for VR
    }
    
    private void CreateMenuBackground()
    {
        GameObject bgObj = new GameObject("Menu_Background");
        bgObj.transform.SetParent(worldCanvas.transform);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
    }
    
    private void CreateMenuTitle()
    {
        GameObject titleObj = new GameObject("Menu_Title");
        titleObj.transform.SetParent(worldCanvas.transform);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "VR Diagnostic Vision Therapy";
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.8f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;
    }
    
    private void CreateCalibrationStatusDisplay()
    {
        GameObject statusObj = new GameObject("Calibration_Status");
        statusObj.transform.SetParent(worldCanvas.transform);
        
        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Calibration: ⚠ Required";
        statusText.fontSize = 24;
        statusText.color = Color.yellow;
        statusText.alignment = TextAlignmentOptions.Center;
        
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.1f, 0.65f);
        statusRect.anchorMax = new Vector2(0.9f, 0.75f);
        statusRect.sizeDelta = Vector2.zero;
        statusRect.anchoredPosition = Vector2.zero;
    }
    
    private void CreateMenuButtons()
    {
        string[] buttonLabels = { "Run Calibration", "Start Diagnostic", "Review Results", "Settings", "Quit" };
        menuButtons = new GameObject[buttonLabels.Length];
        buttonInteractables = new InteractableMenuItem[buttonLabels.Length];
        buttonTexts = new TextMeshProUGUI[buttonLabels.Length];
        
        float startY = 0.55f;
        float buttonHeight = 0.08f;
        
        for (int i = 0; i < buttonLabels.Length; i++)
        {
            GameObject buttonObj = CreateVRButton(buttonLabels[i], i, startY - (i * (buttonHeight + 0.02f)));
            menuButtons[i] = buttonObj;
        }
    }
    
    private GameObject CreateVRButton(string label, int index, float yPosition)
    {
        // Create button container
        GameObject buttonObj = new GameObject($"Button_{label.Replace(" ", "")}");
        buttonObj.transform.SetParent(worldCanvas.transform);
        buttonObj.layer = LayerMask.NameToLayer("UI");
        
        // Add BoxCollider for VR interaction
        BoxCollider buttonCollider = buttonObj.AddComponent<BoxCollider>();
        buttonCollider.size = new Vector3(buttonSize.x, buttonSize.y, 0.1f);
        
        // Add InteractableMenuItem component
        InteractableMenuItem interactable = buttonObj.AddComponent<InteractableMenuItem>();
        buttonInteractables[index] = interactable;
        
        // Create visual background
        GameObject bgObj = new GameObject("Button_Background");
        bgObj.transform.SetParent(buttonObj.transform);
        
        Image buttonImage = bgObj.AddComponent<Image>();
        buttonImage.color = buttonNormalColor;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create button text
        GameObject textObj = new GameObject("Button_Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.fontSize = 32;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonTexts[index] = buttonText;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Position button
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.2f, yPosition);
        buttonRect.anchorMax = new Vector2(0.8f, yPosition + 0.08f);
        buttonRect.sizeDelta = Vector2.zero;
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Add progress indicator
        CreateDwellProgressIndicator(buttonObj, interactable);
        
        return buttonObj;
    }
    
    private void CreateDwellProgressIndicator(GameObject buttonObj, InteractableMenuItem interactable)
    {
        GameObject progressObj = new GameObject("Dwell_Progress");
        progressObj.transform.SetParent(buttonObj.transform);
        
        Image progressImage = progressObj.AddComponent<Image>();
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.color = Color.cyan;
        progressImage.fillAmount = 0f;
        progressObj.SetActive(false);
        
        RectTransform progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.85f, 0.1f);
        progressRect.anchorMax = new Vector2(0.95f, 0.9f);
        progressRect.sizeDelta = Vector2.zero;
        progressRect.anchoredPosition = Vector2.zero;
        
        // Assign to interactable
        interactable.dwellProgressImage = progressImage;
    }
    
    private void ConfigureVRMainMenuComponent()
    {
        if (vrMainMenu == null) return;
        
        // Use reflection or public setters to configure the VRMainMenu component
        // Set canvas reference
        var canvasField = typeof(VRMainMenu).GetField("mainMenuCanvas", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (canvasField != null)
        {
            canvasField.SetValue(vrMainMenu, worldCanvas.gameObject);
        }
        
        // Set button references
        SetButtonReference("runCalibrationButton", 0);
        SetButtonReference("startDiagnosticButton", 1);
        SetButtonReference("reviewResultsButton", 2);
        SetButtonReference("settingsButton", 3);
        SetButtonReference("quitButton", 4);
        
        // Set text references
        SetTextReference("calibrationButtonText", 0);
        SetTextReference("diagnosticButtonText", 1);
        SetTextReference("resultsButtonText", 2);
        SetTextReference("settingsButtonText", 3);
        SetTextReference("quitButtonText", 4);
    }
    
    private void SetButtonReference(string fieldName, int index)
    {
        if (index >= buttonInteractables.Length) return;
        
        var field = typeof(VRMainMenu).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && buttonInteractables[index] != null)
        {
            field.SetValue(vrMainMenu, buttonInteractables[index]);
        }
    }
    
    private void SetTextReference(string fieldName, int index)
    {
        if (index >= buttonTexts.Length) return;
        
        var field = typeof(VRMainMenu).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && buttonTexts[index] != null)
        {
            field.SetValue(vrMainMenu, buttonTexts[index]);
        }
    }
    
    private void UpdateMenuPosition()
    {
        if (menuRoot == null || playerTransform == null) return;
        
        Vector3 forwardDirection = followPlayerRotation ? playerTransform.forward : Vector3.forward;
        Vector3 targetPosition = playerTransform.position + 
                                forwardDirection * menuDistance + 
                                Vector3.up * menuHeight;
        
        menuRoot.transform.position = targetPosition;
        
        if (followPlayerRotation)
        {
            menuRoot.transform.LookAt(playerTransform.position);
            menuRoot.transform.Rotate(0, 180, 0); // Face the player
        }
        else
        {
            menuRoot.transform.rotation = Quaternion.identity;
        }
    }
    
    public void SetMenuDistance(float distance)
    {
        menuDistance = distance;
        UpdateMenuPosition();
    }
    
    public void SetMenuHeight(float height)
    {
        menuHeight = height;
        UpdateMenuPosition();
    }
    
    public void SetFollowPlayerRotation(bool follow)
    {
        followPlayerRotation = follow;
        UpdateMenuPosition();
    }
}