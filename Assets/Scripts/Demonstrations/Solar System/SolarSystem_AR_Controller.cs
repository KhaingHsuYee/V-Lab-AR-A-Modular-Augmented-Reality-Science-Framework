using System.Collections;       
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using UnityEngine.UIElements; 
using UnityEngine.SceneManagement; 
using DG.Tweening; 

public class SolarSystem_AR_Controller : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private GameObject solarSystemPrefab; // Updated prefab name

    [Header("UI Documents")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    [SerializeField] private UIDocument mainHudUIDocument; 

    [Header("Audio & Settings")]
    [SerializeField] private AudioClip dropSoundClip; 
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 3.0f;
    [SerializeField] private float scaleSpeed = 0.01f;
    
    [Header("Scene Navigation")]
    [SerializeField] private string homeSceneName = "MainMenu"; // Set this to your actual Home scene name

    // AR Variables
    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    private GameObject spawnedSystem; 
    private float initialPinchDistance;
    private Vector3 initialScale;

    // UI Variables
    private VisualElement welcomeScreenContainer; 
    private VisualElement hudContainer;
    private VisualElement exitPopupOverlay;
    private Label popupTitle, popupMessage;
    private Button btnCancelExit, btnConfirmExit;
    
    private enum PopupState { None, Home, Reset }
    private PopupState currentPopupState = PopupState.None;

    private void OnEnable()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        InitializeWelcomeUI();
        InitializeMainHUD();
    }

    private void InitializeWelcomeUI()
    {
        if (welcomeUIDocument != null)
        {
            welcomeUIDocument.gameObject.SetActive(true);
            var root = welcomeUIDocument.rootVisualElement;
            if (root != null)
            {
                welcomeScreenContainer = root.Q<VisualElement>("welcome-screen-container"); 
                Button dismissButton = root.Q<Button>("dismiss-button");
                
                // Updated instructions for Solar System
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                if (inst1 != null) inst1.text = "1. Scan your room and tap to place the Solar System.";
                if (inst2 != null) inst2.text = "2. Double-tap a planet to inspect it.";

                if (welcomeScreenContainer != null) 
                {
                    welcomeScreenContainer.style.display = DisplayStyle.Flex;
                    welcomeScreenContainer.style.scale = new StyleScale(new Vector2(0, 0));
                    DOTween.To(() => Vector2.zero, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBack);
                }
                
                if (dismissButton != null) dismissButton.clicked += DismissWelcomePanel;
            }
        }
    }

    private void InitializeMainHUD()
    {
        if (mainHudUIDocument != null)
        {
            mainHudUIDocument.gameObject.SetActive(true);
            var root = mainHudUIDocument.rootVisualElement;
            if (root != null)
            {
                hudContainer = root.Q<VisualElement>("hud-container"); 
                if (hudContainer != null) hudContainer.style.display = DisplayStyle.None;

                Button btnHome = root.Q<Button>("btn-home");
                Button btnReset = root.Q<Button>("btn-reset");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;
            }
        }
    }

    private void DismissWelcomePanel()
    {
        if (welcomeScreenContainer != null)
        {
            DOTween.To(() => Vector2.one, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                welcomeScreenContainer.style.display = DisplayStyle.None;
            });
        }
    }

    void Update()
    {
        if (welcomeScreenContainer != null && welcomeScreenContainer.style.display == DisplayStyle.Flex) return;
        if (exitPopupOverlay != null && exitPopupOverlay.style.display == DisplayStyle.Flex) return;

        if (!equipmentSpawned) HandlePlacement();
        else Handle360Interactions();
    }

    private void HandlePlacement()
    {
        Vector2 inputPosition = Vector2.zero;
        bool inputDetected = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            inputDetected = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPosition = Mouse.current.position.ReadValue();
            inputDetected = true;
        }

        if (inputDetected)
        {
            if (raycastManager.Raycast(inputPosition, hitResults, TrackableType.PlaneWithinPolygon))
            {
                SpawnTableAtPosition(hitResults[0].pose.position, hitResults[0].pose.rotation);
                equipmentSpawned = true; 
            }
        }
    }

    void SpawnTableAtPosition(Vector3 position, Quaternion rotation)
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
        Vector3 spawnPosition = position + (Vector3.up * 0.05f);

        spawnedSystem = Instantiate(solarSystemPrefab, spawnPosition, cameraLookRotation);
        spawnedSystem.AddComponent<UnityEngine.XR.ARFoundation.ARAnchor>();
        
        Vector3 targetScale = spawnedSystem.transform.localScale; 
        spawnedSystem.transform.localScale = Vector3.zero; 
        
        spawnedSystem.transform.DOScale(targetScale, 0.8f).SetEase(Ease.OutElastic).OnComplete(() => {
            
            if (dropSoundClip != null)
            {
                AudioSource src = spawnedSystem.GetComponent<AudioSource>();
                if (src == null) src = spawnedSystem.AddComponent<AudioSource>();
                src.PlayOneShot(dropSoundClip);
            }

            if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;
            
            // Note: We removed the Dashboard Quad glueing logic here.
            // The dashboard now spawns through the SolarSystemInteractionManager on double-tap.
        });

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
        }
    }

    private void Handle360Interactions()
    {
        if (spawnedSystem == null) return;

        // This allows the user to pinch-to-zoom or swipe to rotate the entire orbiting solar system
        if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

            if (touches.Count == 1) 
            {
                if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float rotationAmount = touches[0].delta.x * rotationSpeed;
                    spawnedSystem.transform.Rotate(0, -rotationAmount, 0, Space.World);
                }
            }
            else if (touches.Count == 2) 
            {
                var touch0 = touches[0];
                var touch1 = touches[1];

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began || touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    initialScale = spawnedSystem.transform.localScale;
                }
                else if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float currentPinchDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    float difference = currentPinchDistance - initialPinchDistance;
                    Vector3 newScale = initialScale + (Vector3.one * difference * scaleSpeed);
                    
                    newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                    newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                    newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
                    spawnedSystem.transform.localScale = newScale;
                }
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed) 
            {
                float mouseDeltaX = Mouse.current.delta.x.ReadValue();
                if (Mathf.Abs(mouseDeltaX) > 0.1f)
                {
                    float rotationAmount = mouseDeltaX * (rotationSpeed * 0.5f); 
                    spawnedSystem.transform.Rotate(0, -rotationAmount, 0, Space.World);
                }
            }

            float scrollValue = Mouse.current.scroll.ReadValue().y; 
            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                float zoomDirection = scrollValue > 0 ? 1f : -1f;
                Vector3 newScale = spawnedSystem.transform.localScale + (Vector3.one * zoomDirection * scaleSpeed * 5f);
                
                newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
                
                spawnedSystem.transform.localScale = newScale;
            }
        }
    }

    private void PromptGoHome() 
    { 
        currentPopupState = PopupState.Home; 
        if (popupTitle != null) popupTitle.text = "Exit to Menu?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to return to the main menu?";
        if (btnCancelExit != null) btnCancelExit.text = "Cancel"; 
        if (btnConfirmExit != null) btnConfirmExit.text = "Exit"; 
        ShowPopup(); 
    }

    private void PromptReset() 
    { 
        currentPopupState = PopupState.Reset; 
        if (popupTitle != null) popupTitle.text = "Reset Scene?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to move the Solar System to a new location?";
        if (btnCancelExit != null) btnCancelExit.text = "Cancel"; 
        if (btnConfirmExit != null) btnConfirmExit.text = "Reset"; 
        ShowPopup(); 
    }

    private void ShowPopup() 
    { 
        if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.Flex; 
    }

    private void ClosePopup() 
    { 
        if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.None; 
    }

    private void ConfirmPopupAction()
    {
        if (currentPopupState == PopupState.Home) 
        {
            SceneManager.LoadScene(homeSceneName); 
        }
        else if (currentPopupState == PopupState.Reset) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}