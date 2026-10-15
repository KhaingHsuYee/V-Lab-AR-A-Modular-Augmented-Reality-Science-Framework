using System.Collections;       
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using UnityEngine.UIElements; 
using UnityEngine.SceneManagement; 
using DG.Tweening; 
using UnityEngine.EventSystems;

public class PeriodicTable_AR_Controller : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private GameObject periodicTablePrefab;

    [Header("UI Documents")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    [SerializeField] private UIDocument mainHudUIDocument; 

    [Header("Audio & Settings")]
    [SerializeField] private AudioClip dropSoundClip; 
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 3.0f;
    [SerializeField] private float scaleSpeed = 0.01f;

    [Tooltip("Drag your new PeriodicTable_Dashboard_3D quad here")]
    [SerializeField] private GameObject dashboardQuad;

    [Header("Shadow Settings")]
    [SerializeField] private Material shadowReceiverMaterial; 

    [Header("Atom Generator Setup")]
    [SerializeField] private GameObject atomContainerPrefab; 
    private GameObject spawnedAtom;
    private VisualElement bottomElementMenu;
    private ScrollView elementScrollView;
    private ElementData_Periodic[] allElements;

    // AR Variables
    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    private GameObject spawnedTable; 
    private float initialPinchDistance;
    private Vector3 initialScale;

    // UI Variables
    private VisualElement welcomeScreenContainer; 
    private VisualElement hudContainer;
    private VisualElement exitPopupOverlay;
    private Label popupTitle, popupMessage;
    private Button btnCancelExit, btnConfirmExit;
    private Button currentlySelectedButton;

    private Button btnAtom;
    private bool isAtomView = false;
    
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
                
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                if (inst1 != null) inst1.text = "1. Scan your desk and tap to place the Periodic Table.";
                if (inst2 != null) inst2.text = "2. Swipe to rotate. Pinch to zoom in and out.";

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
                Button btnLearn = root.Q<Button>("btn-learn"); 
                
                // Track the ATOM button
                btnAtom = root.Q<Button>("btn-atom");

                bottomElementMenu = root.Q<VisualElement>("bottom-element-menu");
                elementScrollView = root.Q<ScrollView>("element-scroll-view");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;
                if (btnLearn != null) btnLearn.clicked += OnLearnButtonClicked;
                
                // Point to the new toggle logic
                if (btnAtom != null) btnAtom.clicked += ToggleViewMode;

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;

                allElements = Resources.LoadAll<ElementData_Periodic>("Elements");
                PopulateElementMenu();
            }
        }
    }

    private void ToggleViewMode()
    {
        isAtomView = !isAtomView;

        if (isAtomView)
        {
            // Switch to Atom View
            if (btnAtom != null) btnAtom.text = "TABLE";
            if (bottomElementMenu != null) bottomElementMenu.style.display = DisplayStyle.Flex;
            if (spawnedTable != null) spawnedTable.SetActive(false);
        }
        else
        {
            // Switch back to Table View
            if (btnAtom != null) btnAtom.text = "ATOM";
            if (bottomElementMenu != null) bottomElementMenu.style.display = DisplayStyle.None;
            
            if (spawnedAtom != null) Destroy(spawnedAtom);
            if (spawnedTable != null) spawnedTable.SetActive(true);
            
            // Reset UI states
            if (currentlySelectedButton != null)
            {
                currentlySelectedButton.RemoveFromClassList("active-element-btn");
                currentlySelectedButton = null;
            }
            if (ElementUIManager.Instance != null) ElementUIManager.Instance.HideUI();
        }
    }

     private void PopulateElementMenu()
    {
        if (elementScrollView == null || allElements == null) return;

        System.Array.Sort(allElements, (a, b) => a.protons.CompareTo(b.protons));

        foreach (var element in allElements)
        {
            Button elBtn = new Button();
            
            // Use the Symbol instead of the full name
            elBtn.text = element.symbol;
            
            // Apply the new USS class
            elBtn.AddToClassList("element-btn");
            
            // Apply dynamic category colors to the border and text
            Color catColor = GetCategoryColor(element.category);
            elBtn.style.borderBottomColor = catColor;
            elBtn.style.borderTopColor = catColor;
            elBtn.style.borderLeftColor = catColor;
            elBtn.style.borderRightColor = catColor;
            elBtn.style.color = catColor;
            
            elBtn.clicked += () => 
            {
                // 1. Remove the highlight from the previously selected button (if one exists)
                if (currentlySelectedButton != null)
                {
                    currentlySelectedButton.RemoveFromClassList("active-element-btn");
                }
                
                // 2. Apply the highlight to the newly clicked button
                elBtn.AddToClassList("active-element-btn");
                
                // 3. Store this button as the new active reference
                currentlySelectedButton = elBtn;
                
                // 4. Trigger your existing spawn logic
                SpawnAtomicStructure(element);
            };
            elementScrollView.Add(elBtn);
        }
    }

    // Helper method to match the colors used in your Info Panel
    private Color GetCategoryColor(string category)
    {
        if (string.IsNullOrEmpty(category)) return Color.white;
        string cat = category.ToLower();
        
        if (cat.Contains("nonmetal") || cat.Contains("halogen") || cat.Contains("noble")) return new Color(0.0f, 0.8f, 1.0f); 
        if (cat.Contains("alkali") || cat.Contains("earth")) return new Color(1.0f, 0.5f, 0.2f); 
        if (cat.Contains("transition") || cat.Contains("post-transition") || cat.Contains("metal")) return new Color(0.9f, 0.8f, 0.3f);
        if (cat.Contains("metalloid")) return new Color(0.4f, 0.9f, 0.4f); 
        if (cat.Contains("actinide") || cat.Contains("lanthanide")) return new Color(0.8f, 0.4f, 0.9f); 
        
        return Color.white;
    }

    private void SpawnAtomicStructure(ElementData_Periodic elementData)
    {
        if (spawnedTable == null) return;

        spawnedTable.SetActive(false);
        if (spawnedAtom != null) Destroy(spawnedAtom);

        spawnedAtom = Instantiate(atomContainerPrefab, spawnedTable.transform.position, spawnedTable.transform.rotation);
        
        spawnedAtom.transform.rotation = spawnedTable.transform.rotation * Quaternion.Euler(0f, 0f, 35f);
        spawnedAtom.transform.localScale = spawnedTable.transform.localScale;
        
        AtomGenerator generator = spawnedAtom.GetComponent<AtomGenerator>();
        if (generator != null)
        {
            generator.Initialize(elementData);
        }

        // --- NEW: Trigger the deep UI panel update ---
        if (ElementUIManager.Instance != null)
        {
            ElementUIManager.Instance.UpdateUI(elementData);
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

        // --- NEW: Block AR interactions if the pointer/finger is hovering over UI ---
        if (EventSystem.current != null)
        {
            if (Mouse.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                int touchId = Touchscreen.current.touches[0].touchId.ReadValue();
                if (EventSystem.current.IsPointerOverGameObject(touchId)) return;
            }
        }

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

        spawnedTable = Instantiate(periodicTablePrefab, spawnPosition, cameraLookRotation);
        spawnedTable.AddComponent<UnityEngine.XR.ARFoundation.ARAnchor>();
        
        Vector3 targetScale = spawnedTable.transform.localScale; 
        spawnedTable.transform.localScale = Vector3.zero; 
        
        spawnedTable.transform.DOScale(targetScale, 0.8f).SetEase(Ease.OutElastic).OnComplete(() => {
            
            if (dropSoundClip != null)
            {
                AudioSource src = spawnedTable.GetComponent<AudioSource>();
                if (src == null) src = spawnedTable.AddComponent<AudioSource>();
                src.PlayOneShot(dropSoundClip);
            }

            if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;

            if (dashboardQuad != null)
            {
                dashboardQuad.transform.SetParent(spawnedTable.transform);
                dashboardQuad.transform.localPosition = new Vector3(1.3f, 0.6f, 0.1f); 
                dashboardQuad.transform.localRotation = Quaternion.identity; 
            }
        });

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
                if (line != null) 
                {
                    line.enabled = false;
                }
            }
        }
    }

    private void Handle360Interactions()
    {
        // 1. Determine what we are currently looking at
        GameObject activeTarget = (spawnedAtom != null) ? spawnedAtom : spawnedTable;
        if (activeTarget == null) return;

        if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

            if (touches.Count == 1) 
            {
                if (touches[0].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    // ONLY rotate the table here. The Atom rotates itself via AtomRotator.cs
                    if (activeTarget == spawnedTable)
                    {
                        float rotationAmount = touches[0].delta.x * rotationSpeed;
                        activeTarget.transform.Rotate(0, -rotationAmount, 0, Space.World);
                    }
                }
            }
            else if (touches.Count == 2) 
            {
                var touch0 = touches[0];
                var touch1 = touches[1];

                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began || touch1.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    initialScale = activeTarget.transform.localScale;
                }
                else if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch1.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    float currentPinchDistance = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);
                    float difference = currentPinchDistance - initialPinchDistance;
                    Vector3 newScale = initialScale + (Vector3.one * difference * scaleSpeed);
                    
                    newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                    newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                    newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
                    
                    activeTarget.transform.localScale = newScale;
                    
                    // SYNC THE HIDDEN TABLE SO NEXT ATOM INHERITS CORRECT SCALE
                    if (activeTarget == spawnedAtom) spawnedTable.transform.localScale = newScale; 
                }
            }
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed) 
            {
                if (activeTarget == spawnedTable)
                {
                    float mouseDeltaX = Mouse.current.delta.x.ReadValue();
                    if (Mathf.Abs(mouseDeltaX) > 0.1f)
                    {
                        float rotationAmount = mouseDeltaX * (rotationSpeed * 0.5f); 
                        activeTarget.transform.Rotate(0, -rotationAmount, 0, Space.World);
                    }
                }
            }

            float scrollValue = Mouse.current.scroll.ReadValue().y; 
            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                float zoomDirection = scrollValue > 0 ? 1f : -1f;
                Vector3 newScale = activeTarget.transform.localScale + (Vector3.one * zoomDirection * scaleSpeed * 5f);
                
                newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);
                
                activeTarget.transform.localScale = newScale;
                
                // SYNC THE HIDDEN TABLE SO NEXT ATOM INHERITS CORRECT SCALE
                if (activeTarget == spawnedAtom) spawnedTable.transform.localScale = newScale; 
            }
        }
    }

    private void OnLearnButtonClicked()
    {
        Debug.Log("Learn button clicked! Implement panel opening logic here.");
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
        if (popupMessage != null) popupMessage.text = "Are you sure you want to move the periodic table to a new location?";
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
            SceneManager.LoadScene("Demonstration_Home"); 
        }
        else if (currentPopupState == PopupState.Reset) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}