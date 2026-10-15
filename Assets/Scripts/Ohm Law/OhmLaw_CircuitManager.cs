using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UIElements; 

public class OhmLaw_CircuitManager : MonoBehaviour
{
    [Header("Wire Settings")]
    [SerializeField] private Material wireMaterial;
    [SerializeField] private float wireWidth = 0.005f; 
    [SerializeField] private LayerMask portLayer; 

    [Header("Device Inspector UI")]
    [SerializeField] private UIDocument inspectorUIDocument; 

    [Header("Logic Managers")]
    [SerializeField] private OhmLaw_ResultsManager resultsManager; 

    public bool isWiringMode = false; 
    public System.Action OnCircuitComplete; 
    private bool circuitAlreadyValidated = false;

    private LineRenderer activeWire;
    private Transform startingPort;
    private bool isDrawingWire = false;

    // --- REAL-WORLD PHYSICS VARIABLES ---
    private float currentPowerSupplyVoltage = 0.0f; 
    private float currentResistorValue = 50.0f;     

    private Slider voltageSlider;
    private Transform currentlyInspectedDevice;

    // --- UI ELEMENTS ---
    private VisualElement inspectorPanel;
    private Label titleText;
    private Label readingText;
    private VisualElement connectionsList;
    private Button btnCloseInspector;

    // --- TAB VARIABLES ---
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

    void Start()
    {
        SetupInspectorUI();
    }

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

            btnTabData = root.Q<Button>("btn-tab-data");
            btnTabWiring = root.Q<Button>("btn-tab-wiring");
            contentData = root.Q<VisualElement>("content-data");
            contentWiring = root.Q<VisualElement>("content-wiring");

            btnLocalToggle = root.Q<Button>("btn-local-toggle");
            btnTargetToggle = root.Q<Button>("btn-target-toggle");
            scrollLocalList = root.Q<ScrollView>("scroll-local-list");
            scrollTargetList = root.Q<ScrollView>("scroll-target-list");
            btnMakeConnection = root.Q<Button>("btn-make-connection");

            if (btnLocalToggle != null) btnLocalToggle.clicked += () => ToggleDropdown(scrollLocalList);
            if (btnTargetToggle != null) btnTargetToggle.clicked += () => ToggleDropdown(scrollTargetList);
            if (btnMakeConnection != null) btnMakeConnection.clicked += OnMakeConnectionClicked;

            voltageSlider = root.Q<Slider>("voltage-slider");
            if (voltageSlider != null)
            {
                voltageSlider.RegisterValueChangedCallback(evt => 
                {
                    currentPowerSupplyVoltage = evt.newValue;
                    RefreshLiveReadings(); 
                });
            }

            if (btnCloseInspector != null) btnCloseInspector.clicked += () => inspectorPanel.style.display = DisplayStyle.None;
            if (btnTabData != null) btnTabData.clicked += () => SwitchTab(true);
            if (btnTabWiring != null) btnTabWiring.clicked += () => SwitchTab(false);
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

    public void InitializeCircuitLab()
    {
        isWiringMode = true;
    }

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

        if (!isWiringMode) return;
        if (resultsManager != null && resultsManager.IsUIOpen) return;

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
            if (hitTag == "PowerSupply" || hitTag == "Ammeter" || hitTag == "Voltmeter" || hitTag == "Resistor")
            {
                OpenInspectorForDevice(bodyHit.transform.root);
            }
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
            if (deviceRoot.CompareTag("PowerSupply"))
            {
                voltageSlider.style.display = DisplayStyle.Flex;
                voltageSlider.SetValueWithoutNotify(currentPowerSupplyVoltage); 
            }
            else
            {
                voltageSlider.style.display = DisplayStyle.None;
            }
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

                VisualElement infoContainer = new VisualElement();
                infoContainer.AddToClassList("connection-info-container");

                Label portLabel = new Label($"[+] {localPort.name}");
                portLabel.AddToClassList("port-text");

                string cleanTargetName = GetCleanDeviceName(targetPort.root.name);
                Label targetLabel = new Label($"To: {cleanTargetName} ({targetPort.name})");
                targetLabel.AddToClassList("target-text");

                infoContainer.Add(portLabel);
                infoContainer.Add(targetLabel);

                Button deleteBtn = new Button();
                deleteBtn.text = "X";
                deleteBtn.AddToClassList("btn-delete");
                
                GameObject wireObj = wire.wireLine.gameObject;
                deleteBtn.clicked += () => 
                {
                    DeleteWire(wireObj);
                    OpenInspectorForDevice(deviceRoot); 
                };

                row.Add(infoContainer);
                row.Add(deleteBtn);
                connectionsList.Add(row);
            }
        }

        if (!hasConnections)
        {
            Label emptyLabel = new Label("No wires connected.");
            emptyLabel.AddToClassList("target-text");
            emptyLabel.style.color = Color.white;
            connectionsList.Add(emptyLabel);
        }
    }

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
                string cleanParentName = GetCleanDeviceName(parentDevice.name);
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
        float distanceToPort = Vector3.Distance(Camera.main.transform.position, startingPort.position);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceToPort));
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
                
                WireData newWire = new WireData { wireLine = activeWire, startPort = startingPort, endPort = endingPort };
                completedWires.Add(newWire);
                
                activeWire = null; 
                ValidateCircuit();
                
                if (inspectorPanel != null && inspectorPanel.style.display == DisplayStyle.Flex)
                {
                    OpenInspectorForDevice(startingPort.root);
                }
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
        activeWire.numCapVertices = 5; 
        activeWire.numCornerVertices = 5;
        activeWire.SetPosition(0, startPos);
        activeWire.SetPosition(1, startPos);
    }

    private void ValidateCircuit()
    {
        if (circuitAlreadyValidated) return; 

        if (HasConnection("PowerSupply", "PowerSupply") || HasConnection("Ammeter", "Ammeter") || HasConnection("Voltmeter", "Voltmeter"))
        {
            Debug.LogWarning("Short Circuit Detected: A device is connected to itself!");
            return;
        }

        bool psToAmmeter = HasStrictConnection("PowerSupply", "Port_Red", "Ammeter", "Port_Red");
        
        bool ammeterToResistor1 = HasStrictConnection("Ammeter", "Port_Black", "Resistor", "Port_1");
        bool ammeterToResistor2 = HasStrictConnection("Ammeter", "Port_Black", "Resistor", "Port_2");
        bool ammeterToResistor = ammeterToResistor1 || ammeterToResistor2;

        bool resistorToPs = false;
        if (ammeterToResistor1) resistorToPs = HasStrictConnection("Resistor", "Port_2", "PowerSupply", "Port_Black");
        if (ammeterToResistor2) resistorToPs = HasStrictConnection("Resistor", "Port_1", "PowerSupply", "Port_Black");

        bool isSeriesPerfect = psToAmmeter && ammeterToResistor && resistorToPs;

        bool isVoltmeterPerfect = false;
        if (isSeriesPerfect)
        {
            if (ammeterToResistor1)
            {
                bool vRedCorrect = HasStrictConnection("Voltmeter", "Port_Red", "Resistor", "Port_1");
                bool vBlackCorrect = HasStrictConnection("Voltmeter", "Port_Black", "Resistor", "Port_2");
                isVoltmeterPerfect = vRedCorrect && vBlackCorrect;
            }
            else if (ammeterToResistor2)
            {
                bool vRedCorrect = HasStrictConnection("Voltmeter", "Port_Red", "Resistor", "Port_2");
                bool vBlackCorrect = HasStrictConnection("Voltmeter", "Port_Black", "Resistor", "Port_1");
                isVoltmeterPerfect = vRedCorrect && vBlackCorrect;
            }
        }

        if (isSeriesPerfect && isVoltmeterPerfect)
        {
            Debug.Log("CIRCUIT VALIDATED: Strict Polarity Ohm's Law Loop is Perfect!");
            circuitAlreadyValidated = true;
            OnCircuitComplete?.Invoke(); 
            RefreshLiveReadings();
        }
        else if (isSeriesPerfect && !isVoltmeterPerfect && CountWiresOnDevice("Voltmeter") == 2)
        {
            Debug.LogWarning("Circuit Close! But the Voltmeter polarity is backwards, or not in parallel.");
        }
    }

    private bool HasStrictConnection(string tagA, string portNameA, string tagB, string portNameB)
    {
        foreach (var wire in completedWires)
        {
            if (wire.startPort == null || wire.endPort == null) continue;

            string sTag = wire.startPort.root.tag;
            string sPort = wire.startPort.name;
            string eTag = wire.endPort.root.tag;
            string ePort = wire.endPort.name;

            if ((sTag == tagA && sPort == portNameA && eTag == tagB && ePort == portNameB) ||
                (sTag == tagB && sPort == portNameB && eTag == tagA && ePort == portNameA))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasConnection(string tagA, string tagB)
    {
        foreach (var wire in completedWires)
        {
            if (wire.startPort == null || wire.endPort == null) continue;
            string sTag = wire.startPort.root.tag;
            string eTag = wire.endPort.root.tag;

            if ((sTag == tagA && eTag == tagB) || (sTag == tagB && eTag == tagA)) return true;
        }
        return false;
    }

    private int CountWiresOnDevice(string deviceTag)
    {
        int count = 0;
        foreach (var wire in completedWires)
        {
            if (wire.startPort == null || wire.endPort == null) continue;
            if (wire.startPort.root.tag == deviceTag || wire.endPort.root.tag == deviceTag) count++;
        }
        return count;
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (inspectorPanel != null && inspectorPanel.style.display == DisplayStyle.Flex)
        {
            Vector2 uiPos = RuntimePanelUtils.ScreenToPanel(
                inspectorPanel.panel, 
                new Vector2(screenPos.x, Screen.height - screenPos.y)
            );

            if (inspectorPanel.worldBound.Contains(uiPos))
            {
                return true;
            }
        }
        return false;
    }
    
    private void RefreshLiveReadings()
    {
        if (currentlyInspectedDevice == null) return;
        
        if (circuitAlreadyValidated)
        {
            float rInt = 0.1f;
            float rA = 10.0f;
            float rW = 0.2f;
            float rParasitic = rInt + rA + rW;
            float rTotal = currentResistorValue + rParasitic;
            
            float actualCurrentAmps = (rTotal > 0) ? (currentPowerSupplyVoltage / rTotal) : 0f;
            float current_mA = actualCurrentAmps * 1000f; 
            float actualVoltageDrop = actualCurrentAmps * currentResistorValue;

            if (currentlyInspectedDevice.CompareTag("Ammeter")) 
                readingText.text = $"Current: {current_mA:F1} mA";
            else if (currentlyInspectedDevice.CompareTag("Voltmeter")) 
                readingText.text = $"Voltage: {actualVoltageDrop:F2} V";
            else if (currentlyInspectedDevice.CompareTag("Resistor")) 
                readingText.text = $"Load: {currentResistorValue} Ω";
            else if (currentlyInspectedDevice.CompareTag("PowerSupply"))
                readingText.text = $"Output: {currentPowerSupplyVoltage:F2} V";
        }
        else
        {
            readingText.text = "0.00 (No Circuit)";
        }
    }

    // --- NEW STRING CLEANER UTILITY ---
    private string GetCleanDeviceName(string rawName)
    {
        string clean = rawName.Replace("(Clone)", "")
                              .Replace("_Final", "")
                              .Replace("_final", "")
                              .Replace("_on_base", "")
                              .Trim();
                              
        if (clean.Length > 0)
        {
            clean = char.ToUpper(clean[0]) + clean.Substring(1);
        }
        
        return clean;
    }
}