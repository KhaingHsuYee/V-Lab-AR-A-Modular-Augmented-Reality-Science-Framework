using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class KirchhoffLoop_Starter_AR : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject powerSupplyPrefab;
    [SerializeField] private GameObject voltmeterPrefab;
    [SerializeField] private GameObject resistorPrefab;

    [Header("Logic Managers")]
    [SerializeField] private KirchhoffLoop_CircuitManager circuitManager; 
    [SerializeField] private KirchhoffLoop_ResultsManager resultsManager;

    [Header("UI Documents")]
    [SerializeField] private UIDocument welcomeUIDocument;
    [SerializeField] private UIDocument deviceMenuUIDocument; 
    [SerializeField] private UIDocument experimentUIDocument;
    
    [Header("HUD UI")]
    [SerializeField] private UIDocument mainHudUIDocument;
    [SerializeField] private Texture2D resultsIcon;

    // --- DASHBOARD CODE COMMENTED OUT ---
    // [Header("3D Hologram Dashboard")]
    // public GameObject dashboardGameObject; 
    // public float dashboardHeightOffset = 0.4f;

    [Header("Audio & Interaction")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placementSound;
    [SerializeField] private AudioClip successSound;

    [Header("Instructions UI")]
    [SerializeField] private UIDocument instructionsUIDocument;
    [SerializeField] private Texture2D circuitDiagramTexture;
    private VisualElement instructionsRoot;
    private VisualElement instructionsOverlay;

    private VisualElement welcomeScreenContainer;
    private VisualElement deviceMenuContainer;
    private VisualElement experimentScreenContainer;
    private VisualElement hudContainer;
    private Label statusTextLeft;
    private Button btnHome;
    private Button btnReset;
    private Button btnModeToggle; 
    private VisualElement exitPopupOverlay;
    private VisualElement exitPopupBox;
    private Label popupTitle;        
    private Label popupMessage;      
    private Button btnCancelExit;
    private Button btnConfirmExit;

    private Button dismissButton;
    private Button beginExperimentButton;
    private Button btnSelectPower;
    private Button btnSelectVoltmeter;
    private Button btnSelectResistor;

    [Header("Experiment Instructions")]
    [SerializeField] private string experimentTitle = "Kirchhoff's Loop Rule";
    [SerializeField] private string instruction1 = "1. Place the Power Supply and 3 Resistors.";
    [SerializeField] private string instruction2 = "2. Wire the components into a single series loop.";
    [SerializeField] private string instruction3 = "3. Use the Voltmeter to measure each voltage drop.";

    private enum ActiveDevice { None, PowerSupply, Voltmeter, Resistor }
    private ActiveDevice currentSelection = ActiveDevice.None;
    private enum LabMode { Setup, Wire, Move }
    private LabMode currentLabMode = LabMode.Setup;
    private bool experimentStarted = false;

    private bool placedPower = false;
    private int voltmetersPlaced = 0;
    private int resistorsPlaced = 0;
    
    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private List<Transform> spawnedEquipment = new List<Transform>(); 
    private float uiClickCooldown = 0f; 
    private Transform selectedEquipment = null;
    private float initialRotationAngle = 0f;

    private enum PopupState { None, Home, Reset }
    private PopupState currentPopupState = PopupState.None;

    void Start()
    {
        SetupWelcomeScreen();
        SetupDeviceMenu();
        SetupExperimentScreen();
        SetupMainHUD();
        SetupInstructionsUI(); // ADD THIS LINE
    }

    private void SetupMainHUD()
    {
        if (mainHudUIDocument != null)
        {
            mainHudUIDocument.gameObject.SetActive(true);
            var root = mainHudUIDocument.rootVisualElement;
            if (root != null)
            {
                hudContainer = root.Q<VisualElement>("hud-container");
                if (hudContainer != null) hudContainer.style.display = DisplayStyle.None; 
                statusTextLeft = root.Q<Label>("status-text-left");
                btnHome = root.Q<Button>("btn-home");
                btnReset = root.Q<Button>("btn-reset");
                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                exitPopupBox = root.Q<VisualElement>("exit-popup-box");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;
                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;

                var btnReagent = root.Q<Button>("btn-reagent");
                if (btnReagent != null)
                {
                    btnReagent.clicked += () => { if (resultsManager != null) resultsManager.ToggleResultsPanel(); };
                    if (resultsIcon != null) btnReagent.style.backgroundImage = new StyleBackground(resultsIcon);
                }

                if (hudContainer != null)
                {
                    btnModeToggle = new Button { name = "btn-mode-toggle", text = "MODE: WIRE" };
                    btnModeToggle.style.width = 160; btnModeToggle.style.height = 40;
                    btnModeToggle.style.borderTopLeftRadius = 20; btnModeToggle.style.borderTopRightRadius = 20;
                    btnModeToggle.style.borderBottomLeftRadius = 20; btnModeToggle.style.borderBottomRightRadius = 20;
                    btnModeToggle.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.85f));
                    btnModeToggle.style.color = new StyleColor(Color.white);
                    btnModeToggle.style.fontSize = 14; btnModeToggle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    
                    Color wireColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
                    btnModeToggle.style.borderTopWidth = 2; btnModeToggle.style.borderBottomWidth = 2;
                    btnModeToggle.style.borderLeftWidth = 2; btnModeToggle.style.borderRightWidth = 2;
                    btnModeToggle.style.borderTopColor = new StyleColor(wireColor);
                    btnModeToggle.style.borderBottomColor = new StyleColor(wireColor);
                    btnModeToggle.style.borderLeftColor = new StyleColor(wireColor);
                    btnModeToggle.style.borderRightColor = new StyleColor(wireColor);
                    btnModeToggle.style.position = Position.Absolute;
                    btnModeToggle.style.top = 45; btnModeToggle.style.alignSelf = Align.Center; 
                    btnModeToggle.style.display = DisplayStyle.None; 
                    btnModeToggle.clicked += ToggleLabMode;
                    hudContainer.Add(btnModeToggle);
                }
            }
        }
    }

    private void SetupInstructionsUI()
    {
        if (instructionsUIDocument != null)
        {
            instructionsUIDocument.gameObject.SetActive(true);
            var root = instructionsUIDocument.rootVisualElement;
            if (root != null)
            {
                instructionsRoot = root.Q<VisualElement>("instructions-root");
                instructionsOverlay = root.Q<VisualElement>("instructions-overlay");
                
                // Prevent the invisible container from blocking clicks
                if (instructionsRoot != null) instructionsRoot.pickingMode = PickingMode.Ignore;
                
                Button btnShowInstructions = root.Q<Button>("btn-show-instructions");
                Button btnCloseInstructions = root.Q<Button>("btn-close-instructions");
                VisualElement diagramImage = root.Q<VisualElement>("diagram-image");

                if (diagramImage != null && circuitDiagramTexture != null)
                {
                    diagramImage.style.backgroundImage = new StyleBackground(circuitDiagramTexture);
                }

                if (btnShowInstructions != null) btnShowInstructions.clicked += () => ToggleInstructionOverlay(true);
                if (btnCloseInstructions != null) btnCloseInstructions.clicked += () => ToggleInstructionOverlay(false);
            }
        }
    }

    private void ToggleInstructionOverlay(bool show)
    {
        if (instructionsOverlay != null)
        {
            instructionsOverlay.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void SetupWelcomeScreen()
    {
        if (welcomeUIDocument != null)
        {
            welcomeUIDocument.gameObject.SetActive(true); 
            var root = welcomeUIDocument.rootVisualElement;
            if (root != null)
            {
                welcomeScreenContainer = root.Q<VisualElement>("welcome-screen-container");
                dismissButton = root.Q<Button>("dismiss-button");
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

    private void SetupDeviceMenu()
    {
        if (deviceMenuUIDocument != null)
        {
            deviceMenuUIDocument.gameObject.SetActive(true);
            var root = deviceMenuUIDocument.rootVisualElement;
            if (root != null)
            {
                root.style.display = DisplayStyle.None; 
                deviceMenuContainer = root.Q<VisualElement>("device-menu-container");

                btnSelectPower = root.Q<Button>("btn-power");
                btnSelectVoltmeter = root.Q<Button>("btn-voltmeter");
                btnSelectResistor = root.Q<Button>("btn-resistor");
                Button btnSelectAmmeter = root.Q<Button>("btn-ammeter");
                if (btnSelectAmmeter != null) btnSelectAmmeter.style.display = DisplayStyle.None;

                if (btnSelectPower != null) btnSelectPower.clicked += () => SelectDevice(ActiveDevice.PowerSupply);
                if (btnSelectVoltmeter != null) btnSelectVoltmeter.clicked += () => SelectDevice(ActiveDevice.Voltmeter);
                if (btnSelectResistor != null) btnSelectResistor.clicked += () => SelectDevice(ActiveDevice.Resistor);
            }
        }
    }

    private void SetupExperimentScreen()
    {
        if (experimentUIDocument != null)
        {
            experimentUIDocument.gameObject.SetActive(true);
            var root = experimentUIDocument.rootVisualElement;
            if (root != null)
            {
                experimentScreenContainer = root.Q<VisualElement>(className: "screen-container");
                beginExperimentButton = root.Q<Button>("begin-button");
                
                Label subtitleLabel = root.Q<Label>("subtitle-label");
                if (subtitleLabel != null) subtitleLabel.text = experimentTitle;

                // Query and Set the Instructions
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                Label inst3 = root.Q<Label>("instruction-3");

                if (inst1 != null) inst1.text = instruction1;
                if (inst2 != null) inst2.text = instruction2;
                if (inst3 != null) inst3.text = instruction3;

                if (experimentScreenContainer != null)
                {
                    experimentScreenContainer.style.display = DisplayStyle.None;
                    experimentScreenContainer.style.scale = new StyleScale(new Vector2(0, 0));
                }
                if (beginExperimentButton != null) beginExperimentButton.clicked += StartExperiment;
            }
        }
    }

    private void DismissWelcomePanel()
    {
        if (welcomeScreenContainer != null)
        {
            DOTween.To(() => Vector2.one, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                welcomeScreenContainer.style.display = DisplayStyle.None;
                ShowDeviceMenu();
            });
        }
    }

    private void ShowDeviceMenu()
    {
        if (deviceMenuUIDocument != null)
        {
            deviceMenuUIDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            if (deviceMenuContainer != null) deviceMenuContainer.style.display = DisplayStyle.Flex;
        }
        if (instructionsRoot != null) instructionsRoot.style.display = DisplayStyle.Flex;
    }

    private void SelectDevice(ActiveDevice device) 
    { 
        currentSelection = device; 
        uiClickCooldown = 0.2f; 
    }

    private void ToggleLabMode()
    {
        if (currentLabMode == LabMode.Wire)
        {
            currentLabMode = LabMode.Move;
            if (btnModeToggle != null) 
            {
                btnModeToggle.text = "MODE: MOVE";
                Color moveColor = new Color(1.0f, 0.6f, 0.1f, 1.0f); 
                btnModeToggle.style.borderTopColor = new StyleColor(moveColor);
                btnModeToggle.style.borderBottomColor = new StyleColor(moveColor);
                btnModeToggle.style.borderLeftColor = new StyleColor(moveColor);
                btnModeToggle.style.borderRightColor = new StyleColor(moveColor);
            }
            if (circuitManager != null) circuitManager.isWiringMode = false;
        }
        else if (currentLabMode == LabMode.Move)
        {
            currentLabMode = LabMode.Wire;
            if (btnModeToggle != null) 
            {
                btnModeToggle.text = "MODE: WIRE";
                Color wireColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
                btnModeToggle.style.borderTopColor = new StyleColor(wireColor);
                btnModeToggle.style.borderBottomColor = new StyleColor(wireColor);
                btnModeToggle.style.borderLeftColor = new StyleColor(wireColor);
                btnModeToggle.style.borderRightColor = new StyleColor(wireColor);
            }
            if (circuitManager != null) circuitManager.isWiringMode = true;
        }
    }

    private void PromptGoHome() { currentPopupState = PopupState.Home; if (popupTitle != null) popupTitle.text = "Exit Experiment?"; if (popupMessage != null) popupMessage.text = "Return to main menu?"; ShowPopup(); }
    private void PromptReset() { currentPopupState = PopupState.Reset; if (popupTitle != null) popupTitle.text = "Restart Experiment?"; if (popupMessage != null) popupMessage.text = "Clear desk and start over?"; ShowPopup(); }
    private void ShowPopup() { if (exitPopupOverlay == null || exitPopupBox == null) return; exitPopupOverlay.style.display = DisplayStyle.Flex; exitPopupBox.style.scale = new StyleScale(new Vector2(0, 0)); DOTween.To(() => Vector2.zero, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack); }
    private void ClosePopup() { if (exitPopupOverlay == null || exitPopupBox == null) return; currentPopupState = PopupState.None; DOTween.To(() => Vector2.one, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => { exitPopupOverlay.style.display = DisplayStyle.None; }); }
    private void ConfirmPopupAction() { if (currentPopupState == PopupState.Home) SceneManager.LoadScene("Chemistry_Home"); else if (currentPopupState == PopupState.Reset) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

    void Update()
    {
        if (welcomeScreenContainer != null && welcomeScreenContainer.style.display == DisplayStyle.Flex) return;
        if (experimentScreenContainer != null && experimentScreenContainer.style.display == DisplayStyle.Flex) return;
        if (exitPopupOverlay != null && exitPopupOverlay.style.display == DisplayStyle.Flex) return;
        if (resultsManager != null && resultsManager.IsUIOpen) return;
        if (experimentStarted && currentLabMode != LabMode.Move) return; 

        if (uiClickCooldown > 0)
        {
            uiClickCooldown -= Time.deltaTime;
            return; 
        }

        #if UNITY_EDITOR
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (currentSelection != ActiveDevice.None && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (raycastManager.Raycast(mousePos, hitResults, TrackableType.PlaneWithinPolygon))
                {
                    SpawnSelectedDevice(hitResults[0].pose.position, hitResults[0].pose.rotation);
                }
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (spawnedEquipment.Contains(hit.transform.root)) selectedEquipment = hit.transform.root;
                }
            }
            else if (Mouse.current.leftButton.isPressed && selectedEquipment != null)
            {
                if (raycastManager.Raycast(mousePos, hitResults, TrackableType.PlaneWithinPolygon))
                {
                    selectedEquipment.position = hitResults[0].pose.position;
                }
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame) selectedEquipment = null;

            if (selectedEquipment != null)
            {
                float scroll = Mouse.current.scroll.y.ReadValue();
                if (scroll != 0) selectedEquipment.Rotate(0, (scroll > 0 ? 15f : -15f), 0);
            }
        }
        #endif

        if (Touchscreen.current == null) return;
        bool isTouch0Active = Touchscreen.current.touches.Count > 0 && Touchscreen.current.touches[0].press.isPressed;
        bool isTouch1Active = Touchscreen.current.touches.Count > 1 && Touchscreen.current.touches[1].press.isPressed;

        if (currentSelection != ActiveDevice.None)
        {
            if (Touchscreen.current.touches.Count > 0 && Touchscreen.current.touches[0].press.wasPressedThisFrame)
            {
                Vector2 touchPos = Touchscreen.current.touches[0].position.ReadValue();
                if (raycastManager.Raycast(touchPos, hitResults, TrackableType.PlaneWithinPolygon))
                {
                    SpawnSelectedDevice(hitResults[0].pose.position, hitResults[0].pose.rotation);
                }
            }
            return; 
        }

        if (isTouch0Active && isTouch1Active)
        {
            if (selectedEquipment != null)
            {
                Vector2 touch0Pos = Touchscreen.current.touches[0].position.ReadValue();
                Vector2 touch1Pos = Touchscreen.current.touches[1].position.ReadValue();
                Vector2 currentVector = touch1Pos - touch0Pos;
                float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

                if (Touchscreen.current.touches[1].press.wasPressedThisFrame) initialRotationAngle = currentAngle - selectedEquipment.eulerAngles.y;
                selectedEquipment.rotation = Quaternion.Euler(0, currentAngle - initialRotationAngle, 0);
            }
            return; 
        }

        if (isTouch0Active && !isTouch1Active)
        {
            Vector2 touchPos = Touchscreen.current.touches[0].position.ReadValue();
            if (Touchscreen.current.touches[0].press.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (spawnedEquipment.Contains(hit.transform.root)) selectedEquipment = hit.transform.root; 
                }
            }

            if (selectedEquipment != null && raycastManager.Raycast(touchPos, hitResults, TrackableType.PlaneWithinPolygon))
            {
                selectedEquipment.position = hitResults[0].pose.position;
            }
        }
        if (Touchscreen.current.touches.Count > 0 && Touchscreen.current.touches[0].press.wasReleasedThisFrame) selectedEquipment = null;
    }

    private void SpawnSelectedDevice(Vector3 position, Quaternion rotation)
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
        GameObject newlySpawned = null;

        switch (currentSelection)
        {
            case ActiveDevice.PowerSupply:
                if (!placedPower) 
                { 
                    newlySpawned = Instantiate(powerSupplyPrefab, position, cameraLookRotation); 
                    placedPower = true; 
                    btnSelectPower.SetEnabled(false); 
                }
                break;
            case ActiveDevice.Voltmeter:
                if (voltmetersPlaced < 1) 
                { 
                    newlySpawned = Instantiate(voltmeterPrefab, position, cameraLookRotation); 
                    voltmetersPlaced++; 
                    newlySpawned.name = $"Voltmeter_{voltmetersPlaced}";
                    if (voltmetersPlaced >= 1) btnSelectVoltmeter.SetEnabled(false); 
                }
                break;
            case ActiveDevice.Resistor:
                if (resistorsPlaced < 3) 
                { 
                    newlySpawned = Instantiate(resistorPrefab, position, cameraLookRotation); 
                    resistorsPlaced++; 
                    newlySpawned.name = $"Resistor_{resistorsPlaced}";
                    if (resistorsPlaced >= 3) btnSelectResistor.SetEnabled(false); 
                }
                break;
        }

        // Only play the placement sound if an object was successfully instantiated
        if (newlySpawned != null) 
        {
            if (audioSource != null && placementSound != null) audioSource.PlayOneShot(placementSound);
            spawnedEquipment.Add(newlySpawned.transform);
        }
        
        currentSelection = ActiveDevice.None; 
        CheckAllDevicesPlaced();
    }

    private void CheckAllDevicesPlaced()
    {
        if (placedPower && voltmetersPlaced == 1 && resistorsPlaced == 3)
        {
            if (deviceMenuContainer != null) deviceMenuContainer.style.display = DisplayStyle.None;
            if (planeManager != null)
            {
                planeManager.enabled = false;
                foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(false);
            }
            AnimateExperimentUIPopIn();
        }
    }

    void AnimateExperimentUIPopIn()
    {
        if (experimentScreenContainer != null)
        {
            experimentScreenContainer.style.display = DisplayStyle.Flex;
            DOTween.To(() => Vector2.zero, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBack);
        }
    }

    void StartExperiment()
    {
        if (experimentScreenContainer != null)
        {
            DOTween.To(() => Vector2.one, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    experimentScreenContainer.style.display = DisplayStyle.None;
                    experimentStarted = true;
                    currentLabMode = LabMode.Wire;

                    if (hudContainer != null)
                    {
                        hudContainer.style.display = DisplayStyle.Flex;
                        hudContainer.style.opacity = 0f;
                        DOTween.To(() => 0f, x => hudContainer.style.opacity = x, 1f, 1f);
                        if (statusTextLeft != null) statusTextLeft.text = "Wire the 3 resistors in series, then connect your single voltmeter in parallel across the first one.";
                    }

                    if (btnModeToggle != null)
                    {
                        btnModeToggle.style.display = DisplayStyle.Flex;
                        btnModeToggle.text = "MODE: WIRE";
                    }

                    // --- DASHBOARD CODE COMMENTED OUT ---
                    /*
                    if (dashboardGameObject != null && spawnedEquipment.Count > 0)
                    {
                        Vector3 spawnOffset = (Vector3.up * dashboardHeightOffset) + (Camera.main.transform.forward * 0.1f);
                        dashboardGameObject.transform.position = spawnedEquipment[0].position + spawnOffset;
                        if (Camera.main != null)
                        {
                            Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                            dashboardGameObject.transform.LookAt(targetPosition);
                            dashboardGameObject.transform.Rotate(0, 180, 0); 
                        }
                        dashboardGameObject.SetActive(true);
                    }
                    */

                    if (circuitManager != null)
                    {
                        circuitManager.enabled = true;
                        circuitManager.OnCircuitComplete += HandleCircuitSuccess; 
                        circuitManager.InitializeCircuitLab(); 
                    }
            });
        }
    }

    private void HandleCircuitSuccess()
    {
        if (statusTextLeft != null)
        {
            statusTextLeft.text = "SERIES LOOP VALIDATED! Measure V1, then move the voltmeter wires to measure V2, V3, and V-Total. Record all four in the results panel.";
            statusTextLeft.style.color = new StyleColor(Color.green);
        }
        if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound); 
    }
}