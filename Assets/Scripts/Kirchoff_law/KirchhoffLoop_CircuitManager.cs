using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UIElements; 

public class KirchhoffLoop_CircuitManager : MonoBehaviour
{
    [Header("Wire Settings")]
    [SerializeField] private Material wireMaterial;
    [SerializeField] private float wireWidth = 0.005f; 
    [SerializeField] private LayerMask portLayer; 

    [Header("Device Inspector UI")]
    [SerializeField] private UIDocument inspectorUIDocument; 

    [Header("Logic Managers")]
    [SerializeField] private KirchhoffLoop_ResultsManager resultsManager; 

    public bool isWiringMode = false; 
    public System.Action OnCircuitComplete; 
    private bool circuitAlreadyValidated = false;

    private LineRenderer activeWire;
    private Transform startingPort;
    private bool isDrawingWire = false;

    private float currentPowerSupplyVoltage = 0.0f; 
    private const float R1_Value = 50.0f;
    private const float R2_Value = 100.0f;
    private const float R3_Value = 150.0f;

    private Slider voltageSlider;
    private Transform currentlyInspectedDevice;
    private VisualElement inspectorPanel;
    private Label titleText;
    private Label readingText;
    private VisualElement connectionsList;
    private Button btnCloseInspector;

    private Button btnTabData;
    private Button btnTabWiring;
    private VisualElement contentData;
    private VisualElement contentWiring;

    // --- CUSTOM UI WIRING VARIABLES ---
    private Button btnLocalToggle;
    private Button btnTargetToggle;
    private ScrollView scrollLocalList;
    private ScrollView scrollTargetList;
    private Button btnMakeConnection;
    private string selectedLocalPort = "";
    private string selectedTargetPort = "";
    private Dictionary<string, Transform> localPortMap = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> targetPortMap = new Dictionary<string, Transform>();

    private class WireData 
    {
        public LineRenderer wireLine;
        public Transform startPort;
        public Transform endPort;
    }
    private List<WireData> completedWires = new List<WireData>();

    // PHYSICS ENGINE: Stores the exact voltage at every port in the circuit
    private Dictionary<string, float> portPotentials = new Dictionary<string, float>();

    void Start() { SetupInspectorUI(); }

    private void SetupInspectorUI()
    {
        if (inspectorUIDocument != null)
        {
            var root = inspectorUIDocument.rootVisualElement;
            if (root == null) return; 

            inspectorPanel = root.Q<VisualElement>("inspector-panel");
            titleText = root.Q<Label>("device-title");
            readingText = root.Q<Label>("device-reading");
            connectionsList = root.Q<VisualElement>("connections-list");
            btnCloseInspector = root.Q<Button>("btn-close-inspector");
            voltageSlider = root.Q<Slider>("voltage-slider");

            btnTabData = root.Q<Button>("btn-tab-data");
            btnTabWiring = root.Q<Button>("btn-tab-wiring");
            contentData = root.Q<VisualElement>("content-data");
            contentWiring = root.Q<VisualElement>("content-wiring");

            // Custom UI Wiring Bindings
            btnLocalToggle = root.Q<Button>("btn-local-toggle");
            btnTargetToggle = root.Q<Button>("btn-target-toggle");
            scrollLocalList = root.Q<ScrollView>("scroll-local-list");
            scrollTargetList = root.Q<ScrollView>("scroll-target-list");
            btnMakeConnection = root.Q<Button>("btn-make-connection");

            if (btnLocalToggle != null) btnLocalToggle.clicked += () => ToggleDropdown(scrollLocalList);
            if (btnTargetToggle != null) btnTargetToggle.clicked += () => ToggleDropdown(scrollTargetList);
            if (btnMakeConnection != null) btnMakeConnection.clicked += OnMakeConnectionClicked;

            if (btnTabData != null) btnTabData.clicked += () => SwitchTab(true);
            if (btnTabWiring != null) btnTabWiring.clicked += () => SwitchTab(false);

            if (voltageSlider != null)
            {
                voltageSlider.RegisterValueChangedCallback(evt => 
                {
                    currentPowerSupplyVoltage = evt.newValue;
                    RefreshLiveReadings(); 
                });
            }

            if (btnCloseInspector != null) btnCloseInspector.clicked += () => inspectorPanel.style.display = DisplayStyle.None;
        }
    }

    private void SwitchTab(bool showDataTab)
    {
        if (contentData != null) contentData.style.display = showDataTab ? DisplayStyle.Flex : DisplayStyle.None;
        if (contentWiring != null) contentWiring.style.display = !showDataTab ? DisplayStyle.Flex : DisplayStyle.None;
        if (showDataTab)
        {
            if (btnTabData != null) btnTabData.AddToClassList("tab-active");
            if (btnTabWiring != null) btnTabWiring.RemoveFromClassList("tab-active");
        }
        else
        {
            if (btnTabWiring != null) btnTabWiring.AddToClassList("tab-active");
            if (btnTabData != null) btnTabData.RemoveFromClassList("tab-active");
        }
    }

    public void InitializeCircuitLab() { isWiringMode = true; }

    void Update()
    {
        foreach (var wire in completedWires)
        {
            if (wire.wireLine != null && wire.startPort != null && wire.endPort != null)
            {
                wire.wireLine.SetPosition(0, wire.startPort.position);
                wire.wireLine.SetPosition(1, wire.endPort.position);
            }
        }

        if (!isWiringMode || (resultsManager != null && resultsManager.IsUIOpen)) return;

        Vector2 inputPos = Vector2.zero;
        bool inputBegan = false, inputHeld = false, inputEnded = false;

        #if UNITY_EDITOR
        if (Mouse.current != null)
        {
            inputPos = Mouse.current.position.ReadValue();
            inputBegan = Mouse.current.leftButton.wasPressedThisFrame;
            inputHeld = Mouse.current.leftButton.isPressed;
            inputEnded = Mouse.current.leftButton.wasReleasedThisFrame;
        }
        #else
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            inputPos = touch.position.ReadValue();
            inputBegan = touch.press.wasPressedThisFrame;
            inputHeld = touch.press.isPressed;
            inputEnded = touch.press.wasReleasedThisFrame;
        }
        #endif

        if (inputBegan) 
        {
            if (IsPointerOverUI(inputPos)) return; 
            HandleTouchBegin(inputPos);
        }
        else if (inputHeld && isDrawingWire) HandleTouchDrag(inputPos);
        else if (inputEnded && isDrawingWire) HandleTouchEnd(inputPos);
    }

    private void HandleTouchBegin(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit portHit, Mathf.Infinity, portLayer))
        {
            if (portHit.transform.CompareTag("CircuitPort"))
            {
                startingPort = portHit.transform;
                CreateNewWire(startingPort.position);
                isDrawingWire = true;
                return; 
            }
        }

        if (Physics.Raycast(ray, out RaycastHit bodyHit, Mathf.Infinity))
        {
            string hitTag = bodyHit.transform.root.tag;
            if (hitTag == "PowerSupply" || hitTag == "Voltmeter" || hitTag == "Resistor")
                OpenInspectorForDevice(bodyHit.transform.root);
        }
    }

    private void OpenInspectorForDevice(Transform deviceRoot)
    {
        if (inspectorPanel == null) return;

        currentlyInspectedDevice = deviceRoot;
        inspectorPanel.style.display = DisplayStyle.Flex;
        
        string displayName = deviceRoot.tag.ToUpper();
        if (displayName == "POWERSUPPLY") displayName = "POWER SUPPLY";
        titleText.text = displayName;

        if (voltageSlider != null)
        {
            voltageSlider.style.display = deviceRoot.CompareTag("PowerSupply") ? DisplayStyle.Flex : DisplayStyle.None;
            if (deviceRoot.CompareTag("PowerSupply")) voltageSlider.SetValueWithoutNotify(currentPowerSupplyVoltage); 
        }

        RefreshLiveReadings();
        PopulateDropdowns(deviceRoot);
        
        connectionsList.Clear();
        bool hasConnections = false;

        foreach (var wire in completedWires)
        {
            if (wire.startPort.root == deviceRoot || wire.endPort.root == deviceRoot)
            {
                hasConnections = true;
                Transform localPort = (wire.startPort.root == deviceRoot) ? wire.startPort : wire.endPort;
                Transform targetPort = (wire.startPort.root == deviceRoot) ? wire.endPort : wire.startPort;

                VisualElement row = new VisualElement();
                row.AddToClassList("connection-row");

                string cleanTargetName = targetPort.root.name.Replace("(Clone)", "").Replace("_Final", "");
                Label label = new Label($"[+] {localPort.name} -> {cleanTargetName} ({targetPort.name})");
                label.AddToClassList("port-text");

                Button deleteBtn = new Button { text = "X" };
                deleteBtn.AddToClassList("btn-delete");
                GameObject wireObj = wire.wireLine.gameObject;
                deleteBtn.clicked += () => { DeleteWire(wireObj); OpenInspectorForDevice(deviceRoot); };

                row.Add(label);
                row.Add(deleteBtn);
                connectionsList.Add(row);
            }
        }
        if (!hasConnections) connectionsList.Add(new Label("No wires connected.") { style = { color = Color.white } });
    }

    // --- CUSTOM UI DROPDOWN LOGIC ---
    private void ToggleDropdown(ScrollView listToToggle)
    {
        bool isCurrentlyHidden = listToToggle.style.display == DisplayStyle.None;
        listToToggle.style.display = isCurrentlyHidden ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void PopulateDropdowns(Transform currentDevice)
    {
        localPortMap.Clear();
        targetPortMap.Clear();
        if (scrollLocalList != null) scrollLocalList.Clear();
        if (scrollTargetList != null) scrollTargetList.Clear();
        selectedLocalPort = "";
        selectedTargetPort = "";
        
        if (btnLocalToggle != null) btnLocalToggle.text = "Select Port...";
        if (btnTargetToggle != null) btnTargetToggle.text = "Select Target...";

        foreach (Transform child in currentDevice.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("CircuitPort"))
            {
                string portName = child.name;
                localPortMap[portName] = child;
                
                Button optionBtn = new Button { text = portName };
                optionBtn.AddToClassList("custom-list-item");
                optionBtn.clicked += () => 
                {
                    selectedLocalPort = portName;
                    btnLocalToggle.text = portName;
                    scrollLocalList.style.display = DisplayStyle.None;
                };
                scrollLocalList.Add(optionBtn);
            }
        }

        GameObject[] allPorts = GameObject.FindGameObjectsWithTag("CircuitPort");
        foreach (GameObject portObj in allPorts)
        {
            Transform parentDevice = portObj.transform.root;
            if (parentDevice != currentDevice && parentDevice.name != "CircuitWire_UI") 
            {
                string cleanParentName = parentDevice.name.Replace("(Clone)", "").Replace("_Final", "");
                string displayName = $"{cleanParentName} ({portObj.name})";
                targetPortMap[displayName] = portObj.transform;
                
                Button optionBtn = new Button { text = displayName };
                optionBtn.AddToClassList("custom-list-item");
                optionBtn.clicked += () => 
                {
                    selectedTargetPort = displayName;
                    btnTargetToggle.text = displayName;
                    scrollTargetList.style.display = DisplayStyle.None;
                };
                scrollTargetList.Add(optionBtn);
            }
        }
    }

    private void OnMakeConnectionClicked()
    {
        if (string.IsNullOrEmpty(selectedLocalPort) || string.IsNullOrEmpty(selectedTargetPort)) return;

        if (localPortMap.ContainsKey(selectedLocalPort) && targetPortMap.ContainsKey(selectedTargetPort)) 
        {
            Transform startPort = localPortMap[selectedLocalPort];
            Transform endPort = targetPortMap[selectedTargetPort];
            
            foreach (var wire in completedWires)
            {
                if ((wire.startPort == startPort && wire.endPort == endPort) || 
                    (wire.startPort == endPort && wire.endPort == startPort)) return;
            }

            GameObject wireObj = new GameObject("CircuitWire_UI");
            LineRenderer newWire = wireObj.AddComponent<LineRenderer>();
            newWire.material = wireMaterial;
            newWire.startWidth = wireWidth;
            newWire.endWidth = wireWidth;
            newWire.positionCount = 2;
            newWire.useWorldSpace = true;
            newWire.SetPosition(0, startPort.position);
            newWire.SetPosition(1, endPort.position);

            completedWires.Add(new WireData { wireLine = newWire, startPort = startPort, endPort = endPort });
            
            ValidateCircuit();
            OpenInspectorForDevice(currentlyInspectedDevice); 
        }
    }

    private void DeleteWire(GameObject wireObject)
    {
        WireData wireToRemove = completedWires.Find(w => w.wireLine != null && w.wireLine.gameObject == wireObject);
        if (wireToRemove != null)
        {
            completedWires.Remove(wireToRemove);
            Destroy(wireObject);
            circuitAlreadyValidated = false; 
            RefreshLiveReadings();
        }
    }

    private void HandleTouchDrag(Vector2 screenPos)
    {
        float dist = Vector3.Distance(Camera.main.transform.position, startingPort.position);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
        activeWire.SetPosition(1, worldPos);
    }

    private void HandleTouchEnd(Vector2 screenPos)
    {
        isDrawingWire = false;
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, portLayer))
        {
            if (hit.transform.CompareTag("CircuitPort") && hit.transform != startingPort)
            {
                Transform endingPort = hit.transform;
                activeWire.SetPosition(1, endingPort.position);
                completedWires.Add(new WireData { wireLine = activeWire, startPort = startingPort, endPort = endingPort });
                activeWire = null; 
                ValidateCircuit();
                if (inspectorPanel.style.display == DisplayStyle.Flex) OpenInspectorForDevice(startingPort.root);
                return;
            }
        }
        if (activeWire != null)
        {
            Destroy(activeWire.gameObject);
            activeWire = null;
        }
    }

    private void CreateNewWire(Vector3 startPos)
    {
        GameObject wireObj = new GameObject("CircuitWire");
        activeWire = wireObj.AddComponent<LineRenderer>();
        activeWire.material = wireMaterial;
        activeWire.startWidth = wireWidth;
        activeWire.endWidth = wireWidth;
        activeWire.positionCount = 2;
        activeWire.useWorldSpace = true;
        activeWire.SetPosition(0, startPos);
        activeWire.SetPosition(1, startPos);
    }

    private void ValidateCircuit()
    {
        if (circuitAlreadyValidated) return; 

        if (IsDynamicSeriesLoopValid())
        {
            Debug.Log("SERIES LOOP COMPLETED! Mapping nodal physics...");
            circuitAlreadyValidated = true;
            OnCircuitComplete?.Invoke(); 
            RefreshLiveReadings();
        }
    }

    private bool IsDynamicSeriesLoopValid()
    {
        GameObject psObj = GameObject.FindGameObjectWithTag("PowerSupply");
        if (psObj == null) return false;

        Transform currentDevice = GetNextSeriesDevice(psObj.name, "Port_Red", out string enteredPortName);
        if (currentDevice == null || !currentDevice.CompareTag("Resistor")) return false;

        List<Transform> visitedResistors = new List<Transform>();

        for (int i = 0; i < 3; i++)
        {
            if (currentDevice == null || !currentDevice.CompareTag("Resistor")) return false;
            if (visitedResistors.Contains(currentDevice)) return false; 

            visitedResistors.Add(currentDevice);
            string exitPortName = (enteredPortName == "Port_1") ? "Port_2" : "Port_1";
            Transform nextDevice = GetNextSeriesDevice(currentDevice.name, exitPortName, out enteredPortName);

            if (i == 2)
            {
                return nextDevice != null && nextDevice.CompareTag("PowerSupply") && enteredPortName == "Port_Black";
            }
            
            currentDevice = nextDevice;
        }
        return false;
    }

    private Transform GetNextSeriesDevice(string sourceDeviceName, string sourcePortName, out string targetPortName)
    {
        targetPortName = "";
        foreach (var wire in completedWires)
        {
            Transform potentialTarget = null;
            string potentialTargetPort = "";

            if (wire.startPort.root.name == sourceDeviceName && wire.startPort.name == sourcePortName)
            {
                potentialTarget = wire.endPort.root;
                potentialTargetPort = wire.endPort.name;
            }
            else if (wire.endPort.root.name == sourceDeviceName && wire.endPort.name == sourcePortName)
            {
                potentialTarget = wire.startPort.root;
                potentialTargetPort = wire.startPort.name;
            }

            // Exclude voltmeter wires so we only trace the main series flow path
            if (potentialTarget != null && !potentialTarget.CompareTag("Voltmeter"))
            {
                targetPortName = potentialTargetPort;
                return potentialTarget;
            }
        }
        return null;
    }

    // Helper: Gets the EXACT port connected to the source port (used by Voltmeter)
    private Transform GetTargetFromPort(string sourceDeviceName, string sourcePortName, out string targetPortName)
    {
        targetPortName = "";
        foreach (var wire in completedWires)
        {
            if (wire.startPort.root.name == sourceDeviceName && wire.startPort.name == sourcePortName)
            {
                targetPortName = wire.endPort.name;
                return wire.endPort.root;
            }
            if (wire.endPort.root.name == sourceDeviceName && wire.endPort.name == sourcePortName)
            {
                targetPortName = wire.startPort.name;
                return wire.startPort.root;
            }
        }
        return null;
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (inspectorPanel != null && inspectorPanel.style.display == DisplayStyle.Flex)
        {
            Vector2 uiPos = RuntimePanelUtils.ScreenToPanel(inspectorPanel.panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
            if (inspectorPanel.worldBound.Contains(uiPos)) return true;
        }
        return false;
    }

    // PHYSICS ENGINE: Calculate exact voltage at every node in the series loop
    private void CalculateNodePotentials()
    {
        portPotentials.Clear();
        GameObject psObj = GameObject.FindGameObjectWithTag("PowerSupply");
        if (psObj == null) return;

        // Set Source Rails
        portPotentials[$"{psObj.name}_Port_Red"] = currentPowerSupplyVoltage;
        portPotentials[$"{psObj.name}_Port_Black"] = 0f;

        float currentPotential = currentPowerSupplyVoltage;
        float totalCurrent = currentPowerSupplyVoltage / (R1_Value + R2_Value + R3_Value);

        // Walk the loop and calculate voltage drops mathematically
        Transform currentDevice = GetNextSeriesDevice(psObj.name, "Port_Red", out string enteredPort);
        
        for (int i = 0; i < 3; i++)
        {
            if (currentDevice == null) break;

            // Voltage entering this resistor
            portPotentials[$"{currentDevice.name}_{enteredPort}"] = currentPotential;
            
            float rValue = 0;
            if (currentDevice.name == "Resistor_1") rValue = R1_Value;
            else if (currentDevice.name == "Resistor_2") rValue = R2_Value;
            else if (currentDevice.name == "Resistor_3") rValue = R3_Value;

            // Voltage drops across the component (V = IR)
            currentPotential -= (totalCurrent * rValue);

            // Voltage exiting this resistor
            string exitPort = (enteredPort == "Port_1") ? "Port_2" : "Port_1";
            portPotentials[$"{currentDevice.name}_{exitPort}"] = currentPotential;

            currentDevice = GetNextSeriesDevice(currentDevice.name, exitPort, out enteredPort);
        }
    }
    
    private void RefreshLiveReadings()
    {
        if (currentlyInspectedDevice == null) return;
        
        if (circuitAlreadyValidated)
        {
            CalculateNodePotentials();

            if (currentlyInspectedDevice.CompareTag("Voltmeter")) 
            {
                Transform redTarget = GetTargetFromPort(currentlyInspectedDevice.name, "Port_Red", out string redTargetPort);
                Transform blackTarget = GetTargetFromPort(currentlyInspectedDevice.name, "Port_Black", out string blackTargetPort);

                if (redTarget != null && blackTarget != null)
                {
                    string redKey = $"{redTarget.name}_{redTargetPort}";
                    string blackKey = $"{blackTarget.name}_{blackTargetPort}";

                    if (portPotentials.ContainsKey(redKey) && portPotentials.ContainsKey(blackKey))
                    {
                        // True physics: Potential difference (V_red - V_black)
                        float vDiff = portPotentials[redKey] - portPotentials[blackKey];
                        readingText.text = $"Voltage: {vDiff:F2} V";
                    }
                    else
                    {
                        readingText.text = "0.00 V (Nodes Not Powered)";
                    }
                }
                else
                {
                    readingText.text = "0.00 V (Incomplete Circuit)";
                }
            }
            else if (currentlyInspectedDevice.CompareTag("Resistor")) 
            {
                if (currentlyInspectedDevice.name == "Resistor_1") readingText.text = $"Load: {R1_Value} Ω";
                else if (currentlyInspectedDevice.name == "Resistor_2") readingText.text = $"Load: {R2_Value} Ω";
                else if (currentlyInspectedDevice.name == "Resistor_3") readingText.text = $"Load: {R3_Value} Ω";
            }
            else if (currentlyInspectedDevice.CompareTag("PowerSupply"))
            {
                readingText.text = $"Output: {currentPowerSupplyVoltage:F2} V";
            }
        }
        else
        {
            readingText.text = "0.00 (No Circuit)";
        }
    }
}