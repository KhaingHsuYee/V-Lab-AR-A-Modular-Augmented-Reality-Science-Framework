using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using DG.Tweening;

[System.Serializable]
public struct MoleculePrefabMapping
{
    public string moleculeName; 
    public GameObject prefab;   
}

public class AR_MoleculeSpawner : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private GameObject placementReticle;
    [SerializeField] private Material shadowReceiverMaterial; 

    [Header("UI Documents")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    [SerializeField] private UIDocument mainHudUIDocument; 

    [Header("Audio & Settings")]
    [SerializeField] private AudioClip dropSoundClip; 
    [SerializeField] private string homeSceneName = "Chemistry_Home"; 

    [Header("Molecule Database")]
    public List<MoleculePrefabMapping> moleculePrefabs;

    [Header("Interaction Settings")]
    [SerializeField] private float autoRotationSpeed = 15f; 
    [SerializeField] private float manualRotationSpeed = 0.2f; 
    [SerializeField] private float minScale = 0.05f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private float scaleSpeed = 0.01f;

    // Interaction State Trackers
    private Vector2 lastSingleTouchPosition;
    private float lastPinchDistance;
    private bool isDragging = false;
    private bool wasPinching = false;
    private float initialPinchDistance;
    private Vector3 initialScale;

    // State Trackers
    private GameObject objectToSpawn; 
    private GameObject spawnedMolecule; 
    private ARAnchor stableAnchor; 
    
    private string currentMoleculeName; // <--- ADDED: Remembers the name of the selected molecule
    
    private bool isTargeting = false;
    private bool isValidPlacement = false; 
    private bool equipmentSpawned = false; 
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

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
        
        if (placementReticle != null) placementReticle.SetActive(false);
    }

    // ==========================================
    // UI INITIALIZATION
    // ==========================================

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
                
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                if (inst1 != null) inst1.text = "1. Select a molecule from the bottom menu.";
                if (inst2 != null) inst2.text = "2. Tap your desk to establish your Lab Station.";

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
                
                if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;

                Molecular_MenuController menuController = Object.FindFirstObjectByType<Molecular_MenuController>();
                if (menuController != null)
                {
                    menuController.EnableMenuButton();
                }
            });
        }
    }

    // ==========================================
    // AR LOGIC & SPAWNING
    // ==========================================

    void Update()
    {
        if (welcomeScreenContainer != null && welcomeScreenContainer.style.display == DisplayStyle.Flex) return;
        if (exitPopupOverlay != null && exitPopupOverlay.style.display == DisplayStyle.Flex) return;

        if (equipmentSpawned)
        {
            Handle360Interactions();
        }
        else if (isTargeting && objectToSpawn != null)
        {
            UpdateReticlePosition();
            HandlePlacementTap();
        }
    }

    public void StartTargeting(string selectedMoleculeName)
    {
        // <--- ADDED: Save the name so we can look up the info later
        currentMoleculeName = selectedMoleculeName; 
        
        objectToSpawn = null;
        foreach (var mapping in moleculePrefabs)
        {
            if (mapping.moleculeName == selectedMoleculeName)
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
            else
            {
                SwapMoleculeAtAnchor();
            }
        }
    }

    private void UpdateReticlePosition()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            isValidPlacement = true;
            
            if (!placementReticle.activeSelf) placementReticle.SetActive(true);

            Pose hitPose = hits[0].pose;
            placementReticle.transform.position = hitPose.position;
            placementReticle.transform.rotation = hitPose.rotation * Quaternion.Euler(90f, 0f, 0f);
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
            Quaternion uprightRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);

            spawnedMolecule = Instantiate(objectToSpawn, placementReticle.transform.position, uprightRotation);
            stableAnchor = spawnedMolecule.AddComponent<ARAnchor>();
            
            AnimateMoleculeSpawn(spawnedMolecule);

            isTargeting = false;
            isValidPlacement = false;
            equipmentSpawned = true;
            placementReticle.SetActive(false);

            LockAndHidePlanes();

            // <--- ADDED: Show the Info Panel exactly when the molecule is placed
            Molecular_MenuController menu = Object.FindFirstObjectByType<Molecular_MenuController>();
            if (menu != null)
            {
                menu.ShowMoleculeInfo(currentMoleculeName);
            }
        }
    }

    private void SwapMoleculeAtAnchor()
    {
        if (stableAnchor == null) return;

        if (spawnedMolecule != null) Destroy(spawnedMolecule);

        spawnedMolecule = Instantiate(objectToSpawn, stableAnchor.transform.position, stableAnchor.transform.rotation);
        stableAnchor = spawnedMolecule.AddComponent<ARAnchor>();

        AnimateMoleculeSpawn(spawnedMolecule);

        // <--- ADDED: Update the Info Panel if they swap molecules
        Molecular_MenuController menu = Object.FindFirstObjectByType<Molecular_MenuController>();
        if (menu != null)
        {
            menu.ShowMoleculeInfo(currentMoleculeName);
        }
    }

    private void AnimateMoleculeSpawn(GameObject molecule)
    {
        Vector3 targetScale = molecule.transform.localScale; 
        molecule.transform.localScale = Vector3.zero; 
        
        molecule.transform.DOScale(targetScale, 0.6f).SetEase(Ease.OutElastic).OnComplete(() => {
            if (dropSoundClip != null)
            {
                AudioSource src = molecule.GetComponent<AudioSource>();
                if (src == null) src = molecule.AddComponent<AudioSource>();
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
                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                if (meshRenderer != null && shadowReceiverMaterial != null)
                {
                    meshRenderer.material = shadowReceiverMaterial;
                }

                LineRenderer line = plane.GetComponent<LineRenderer>();
                if (line != null) line.enabled = false;
            }
        }
    }

    // ==========================================
    // HUD POPUP LOGIC (Home / Reset)
    // ==========================================

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
        if (popupMessage != null) popupMessage.text = "Are you sure you want to move the lab to a new location?";
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

    private void Handle360Interactions()
    {
        if (spawnedMolecule == null) return;

        bool isUserInteracting = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touches = Touchscreen.current.touches;
            bool touch0Pressed = touches.Count > 0 && touches[0].press.isPressed;
            bool touch1Pressed = touches.Count > 1 && touches[1].press.isPressed;

            if (touch0Pressed || touch1Pressed) isUserInteracting = true;

            if (touch0Pressed && touch1Pressed)
            {
                isDragging = false; 

                Vector2 touch0Pos = touches[0].position.ReadValue();
                Vector2 touch1Pos = touches[1].position.ReadValue();
                float currentDistance = Vector2.Distance(touch0Pos, touch1Pos);

                if (wasPinching)
                {
                    float distanceDelta = currentDistance - lastPinchDistance;
                    ApplyScale(distanceDelta * scaleSpeed);
                }

                lastPinchDistance = currentDistance;
                wasPinching = true;
            }
            else if (touch0Pressed && !touch1Pressed)
            {
                wasPinching = false;
                Vector2 currentPos = touches[0].position.ReadValue();
                bool wasPressedThisFrame = touches[0].press.wasPressedThisFrame;
                bool wasReleasedThisFrame = touches[0].press.wasReleasedThisFrame;

                if (wasPressedThisFrame)
                {
                    isDragging = true;
                    lastSingleTouchPosition = currentPos;
                }
                else if (wasReleasedThisFrame)
                {
                    isDragging = false;
                }
                else if (isDragging)
                {
                    RotateObject(currentPos);
                }
            }
            else
            {
                isDragging = false;
                wasPinching = false;
            }
        }
        else if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                isUserInteracting = true;
                ApplyScale(scroll * scaleSpeed * 0.1f); 
            }

            bool isPressed = Mouse.current.leftButton.isPressed;
            bool wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            bool wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            Vector2 currentPos = Mouse.current.position.ReadValue();

            if (isPressed) isUserInteracting = true;

            if (wasPressedThisFrame)
            {
                isDragging = true;
                lastSingleTouchPosition = currentPos;
            }
            else if (wasReleasedThisFrame)
            {
                isDragging = false;
            }
            else if (isDragging && isPressed)
            {
                RotateObject(currentPos);
            }
        }
        else
        {
            isDragging = false;
            wasPinching = false;
        }

        if (!isUserInteracting)
        {
            spawnedMolecule.transform.Rotate(Vector3.up, autoRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void RotateObject(Vector2 currentPos)
    {
        Vector2 delta = currentPos - lastSingleTouchPosition;
        
        spawnedMolecule.transform.Rotate(Vector3.up, -delta.x * manualRotationSpeed, Space.World);
        
        if (Camera.main != null)
        {
            spawnedMolecule.transform.Rotate(Camera.main.transform.right, delta.y * manualRotationSpeed, Space.World);
        }
        
        lastSingleTouchPosition = currentPos;
    }

    private void ApplyScale(float scaleDelta)
    {
        Vector3 newScale = spawnedMolecule.transform.localScale + (Vector3.one * scaleDelta);
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
        spawnedMolecule.transform.localScale = newScale;
    }
}