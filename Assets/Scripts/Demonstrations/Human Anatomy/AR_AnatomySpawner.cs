using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

[System.Serializable]
public struct AnatomyPrefabMapping { public string systemName; public GameObject prefab; }

public class AR_AnatomySpawner : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private GameObject placementReticle;

    [Header("UI Documents")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    [SerializeField] private UIDocument mainHudUIDocument; 

    [Header("Audio & Settings")]
    [SerializeField] private AudioClip dropSoundClip; 
    [SerializeField] private string homeSceneName = "Anatomy_Home"; 

    [Header("Anatomy Database")]
    public List<AnatomyPrefabMapping> anatomyPrefabs;

    [Header("Interaction Settings")]
    [SerializeField] private float autoRotationSpeed = 15f; 
    [SerializeField] private float manualRotationSpeed = 0.2f; 
    [SerializeField] private float minScale = 0.01f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private float scaleSpeed = 0.01f;

    // Interaction & Rotation Trackers
    private float initialPinchDistance;
    private Vector3 initialScale;
    private float currentYAngle = 0f;
    private float autoDirection = 1f;
    private bool isAutoRotating = true;

    private GameObject objectToSpawn; 
    private GameObject spawnedModel; 
    private ARAnchor stableAnchor; 
    private string currentSystemName; 
    
    private bool isTargeting = false;
    private bool isValidPlacement = false; 
    private bool equipmentSpawned = false; 
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private VisualElement welcomeScreenContainer; 
    private VisualElement hudContainer;
    private VisualElement exitPopupOverlay;

    private void OnEnable() => UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
    private void OnDisable() => UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();

    public void SetAutoRotation(bool state) { isAutoRotating = state; }

    void Start()
    {
        InitializeWelcomeUI();
        InitializeMainHUD();
        if (placementReticle != null) placementReticle.SetActive(false);
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
                
                if (welcomeScreenContainer != null) 
                {
                    welcomeScreenContainer.style.display = DisplayStyle.Flex;
                    welcomeScreenContainer.style.scale = new StyleScale(new Vector2(0, 0));
                    DOTween.To(() => Vector2.zero, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBack);
                }
                if (dismissButton != null) dismissButton.clicked += () => {
                    DOTween.To(() => Vector2.one, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                        welcomeScreenContainer.style.display = DisplayStyle.None;
                        if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;
                        Object.FindFirstObjectByType<Anatomy_MenuController>()?.EnableMenuButton();
                    });
                };
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

                root.Q<Button>("btn-home")?.RegisterCallback<ClickEvent>(ev => { if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.Flex; });
                root.Q<Button>("btn-reset")?.RegisterCallback<ClickEvent>(ev => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay") ?? root.Q<VisualElement>("popup-overlay");
                root.Q<Button>("btn-cancel-exit")?.RegisterCallback<ClickEvent>(ev => { if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.None; });
                root.Q<Button>("btn-confirm-exit")?.RegisterCallback<ClickEvent>(ev => SceneManager.LoadScene(homeSceneName));
            }
        }
    }

    void Update()
    {
        if (welcomeScreenContainer != null && welcomeScreenContainer.style.display == DisplayStyle.Flex) return;
        if (exitPopupOverlay != null && exitPopupOverlay.style.display == DisplayStyle.Flex) return;

        if (equipmentSpawned) HandleInteractions();
        else if (isTargeting && objectToSpawn != null)
        {
            UpdateReticlePosition();
            HandlePlacementTap();
        }
    }

    public void StartTargeting(string systemName)
    {
        currentSystemName = systemName; 
        objectToSpawn = null;
        
        foreach (var mapping in anatomyPrefabs)
        {
            if (mapping.systemName.Trim() == systemName.Trim())
            {
                objectToSpawn = mapping.prefab;
                break;
            }
        }

        if (objectToSpawn != null)
        {
            if (!equipmentSpawned)
            {
                isTargeting = true;
                if (placementReticle != null) placementReticle.SetActive(true);
            }
            else SwapModelAtAnchor();
        }
    }

    private void UpdateReticlePosition()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            isValidPlacement = true;
            if (!placementReticle.activeSelf) placementReticle.SetActive(true);
            placementReticle.transform.position = hits[0].pose.position;
            placementReticle.transform.rotation = hits[0].pose.rotation * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            isValidPlacement = false;
            if (placementReticle.activeSelf) placementReticle.SetActive(false);
        }
    }

    private void HandlePlacementTap()
    {
        bool wasPressed = false;
        
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            wasPressed = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            wasPressed = true;

        if (wasPressed && isValidPlacement)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0; 
            
            spawnedModel = Instantiate(objectToSpawn, placementReticle.transform.position, Quaternion.LookRotation(cameraForward, Vector3.up));
            stableAnchor = spawnedModel.AddComponent<ARAnchor>();
            currentYAngle = 0f;
            
            AnimateModelSpawn(spawnedModel);

            isTargeting = false;
            isValidPlacement = false;
            equipmentSpawned = true;
            if (placementReticle != null) placementReticle.SetActive(false);

            LockAndHidePlanes();
            var menuController = Object.FindFirstObjectByType< Anatomy_MenuController >();
            if (menuController != null)
            {
                menuController.ShowSystemInfo(currentSystemName);
                menuController.ShowOrganInfo(currentSystemName);
            }
        }
    }

    private void SwapModelAtAnchor()
    {
        if (stableAnchor == null) return;
        
        // Save the original position before destroying the old model
        Vector3 anchorPosition = stableAnchor.transform.position;
        
        if (spawnedModel != null) Destroy(spawnedModel);

        // Calculate a fresh rotation so it faces the user perfectly, ignoring previous rotations
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 

        // Spawn using the new rotation
        spawnedModel = Instantiate(objectToSpawn, anchorPosition, Quaternion.LookRotation(cameraForward, Vector3.up));
        
        stableAnchor = spawnedModel.AddComponent< ARAnchor >();
        currentYAngle = 0f;
        
        AnimateModelSpawn(spawnedModel);
        
        var menuController = Object.FindFirstObjectByType< Anatomy_MenuController >();
        if (menuController != null)
        {
            menuController.ShowSystemInfo(currentSystemName);
            menuController.ShowOrganInfo(currentSystemName);
        }
    }

    private void AnimateModelSpawn(GameObject model)
    {
        Vector3 targetScale = model.transform.localScale; 
        model.transform.localScale = Vector3.zero; 
        
        model.transform.DOScale(targetScale, 0.6f).SetEase(Ease.OutElastic).OnComplete(() => {
            if (dropSoundClip != null)
            {
                AudioSource src = model.GetComponent<AudioSource>();
                if (src == null) src = model.AddComponent<AudioSource>();
                src.PlayOneShot(dropSoundClip);
            }
        });
    }

    private void LockAndHidePlanes()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false; 
            foreach (var plane in planeManager.trackables) 
            {
                // Physically destroys the visual plane rendering so dots vanish permanently
                plane.gameObject.SetActive(false); 
            }
        }
    }

    // ==========================================
    // SEAMLESS 180 DEGREE INTERACTION LOGIC
    // ==========================================
    private void HandleInteractions()
    {
        if (spawnedModel == null) return;
        
        // Safely blocks model interaction if touching the UI
        if (Anatomy_MenuController.isHoveringUI) return; 

        bool isUserInteracting = false;

        if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

            if (touches.Count == 1) 
            {
                isUserInteracting = true;
                if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float rotY = touches[0].delta.x * manualRotationSpeed;
                    float rotX = touches[0].delta.y * manualRotationSpeed;
                    
                    // Always rotate left/right for everything
                    spawnedModel.transform.Rotate(Vector3.up, -rotY, Space.World);
                    
                    // ONLY rotate up/down if the model is tagged as an Organ
                    if (spawnedModel.CompareTag("Organ"))
                    {
                        spawnedModel.transform.Rotate(Camera.main.transform.right, rotX, Space.World);
                    }
                }
            }
            else if (touches.Count == 2) 
            {
                isUserInteracting = true;
                var touch0 = touches[0];
                var touch1 = touches[1];

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began || touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    initialScale = spawnedModel.transform.localScale;
                }
                else if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float difference = Vector2.Distance(touch0.screenPosition, touch1.screenPosition) - initialPinchDistance;
                    ApplyScale(difference, true);
                }
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed) 
            {
                isUserInteracting = true;
                float rotY = Mouse.current.delta.x.ReadValue() * (manualRotationSpeed * 0.5f);
                float rotX = Mouse.current.delta.y.ReadValue() * (manualRotationSpeed * 0.5f);
                
                // Always rotate left/right for everything
                spawnedModel.transform.Rotate(Vector3.up, -rotY, Space.World);
                
                // ONLY rotate up/down if the model is tagged as an Organ
                if (spawnedModel.CompareTag("Organ"))
                {
                    spawnedModel.transform.Rotate(Camera.main.transform.right, rotX, Space.World);
                }
            }
            float scrollValue = Mouse.current.scroll.ReadValue().y; 
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                isUserInteracting = true;
                ApplyScale(scrollValue > 0 ? 5f : -5f, false);
            }
        }

        // Auto-rotates endlessly in one direction when idle
        if (!isUserInteracting && isAutoRotating)
        {
            spawnedModel.transform.Rotate(0, autoRotationSpeed * Time.deltaTime, 0, Space.World);
        }
    }

    

    private void ApplyClampedRotation(float deltaX)
    {
        currentYAngle -= deltaX;
        // Permits exactly 90 degrees of Left rotation and 90 degrees of Right rotation
        currentYAngle = Mathf.Clamp(currentYAngle, -90f, 90f); 
        spawnedModel.transform.rotation = stableAnchor.transform.rotation * Quaternion.Euler(0, currentYAngle, 0);
    }
    private void ApplyScale(float scaleDelta, bool isPinching)
    {
        Vector3 baseScale = isPinching ? initialScale : spawnedModel.transform.localScale;
        Vector3 newScale = baseScale * (1f + (scaleDelta * scaleSpeed));
        
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        
        spawnedModel.transform.localScale = newScale;
    }

    private void ApplyScale(float scaleDelta)
    {
        Vector3 baseScale = initialScale == Vector3.zero ? spawnedModel.transform.localScale : initialScale;
        Vector3 newScale = baseScale * (1f + (scaleDelta * scaleSpeed));
        
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        
        spawnedModel.transform.localScale = newScale;
    }
}