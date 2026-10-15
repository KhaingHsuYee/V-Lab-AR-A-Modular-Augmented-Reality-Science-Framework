using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DynamicSaltMenuGenerator : MonoBehaviour
{
    [System.Serializable]
    public class SaltData
    {
        public string displayName; // The name displayed on the new UI (e.g., "Sodium Chloride")
        public string formula;     // Must match the exact text of your old buttons (e.g., "NaCl", "KCl", "LiCl", "CuCl2")
    }

    [Header("Menu Database")]
    public SaltData[] saltsInfo = new SaltData[]
    {
        new SaltData { displayName = "Lithium Chloride", formula = "LiCl" },
        new SaltData { displayName = "Sodium Chloride", formula = "NaCl" },
        new SaltData { displayName = "Copper (II) Chloride", formula = "CuCl2" },
        new SaltData { displayName = "Potassium Chloride", formula = "KCl" }
    };

    private GameObject menuPanel;
    private Transform contentContainer;

    void Start()
    {
        BuildInterfaceFromCode();
    }

    private void BuildInterfaceFromCode()
    {
        // 1. Core Render Canvas
        GameObject canvasObj = new GameObject("CodeARCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560, 1080); // Strict wide landscape context

        // 2. Translucent Sidebar Panel (Anchored Left Edge)
        menuPanel = new GameObject("Salt_Menu_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuPanel.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRt = menuPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0, 0);
        panelRt.anchorMax = new Vector2(0, 1);
        panelRt.pivot = new Vector2(0, 0.5f);
        panelRt.anchoredPosition = new Vector2(50, 0); 
        panelRt.sizeDelta = new Vector2(480, -100); // 480px width panel with safety padding top/bottom
        
        // Dark translucent theme matching the target design
        menuPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.88f);

        // 3. Header Section Panel Setup
        CreateHeaderTextElement("Header_Title", "SALT MENU", 26, FontStyles.Bold, new Vector2(30, -40));
        CreateHeaderTextElement("Header_Sub", "SALT SELECTION", 16, FontStyles.Normal, new Vector2(30, -90), new Color(0.6f, 0.6f, 0.6f));

        // 4. Viewport Mask Container for Dynamic List Sliding
        GameObject scrollViewObj = new GameObject("Scroll_View", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect), typeof(Image));
        scrollViewObj.transform.SetParent(menuPanel.transform, false);
        
        RectTransform scrollRt = scrollViewObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(20, 20);      // Margin from bottom edge
        scrollRt.offsetMax = new Vector2(-20, -140);   // Space cleared to avoid overlaying header text
        scrollViewObj.GetComponent<Image>().color = Color.clear;

        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        viewportObj.GetComponent<Mask>().showMaskGraphic = false;
        viewportObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        viewportObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        viewportObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // 5. Layout Engine Box Setup
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12; // Gap spacing between rows
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        contentObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollEngine = scrollViewObj.GetComponent<ScrollRect>();
        scrollEngine.content = contentRt;
        scrollEngine.viewport = viewportObj.GetComponent<RectTransform>();
        scrollEngine.horizontal = false; // Lock out horizontal tracking entirely
        scrollEngine.vertical = true;
        
        contentContainer = contentObj.transform;

        // 6. Generate Rows
        foreach (SaltData salt in saltsInfo)
        {
            GenerateRowButton(salt);
        }
    }

    private void GenerateRowButton(SaltData data)
    {
        // Dynamic Compound Row Base
        GameObject rowObj = new GameObject($"Item_{data.formula}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rowObj.transform.SetParent(contentContainer, false);
        rowObj.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.4f); // Unselected row background color
        
        rowObj.AddComponent<LayoutElement>().preferredHeight = 90; // Balanced vertical thickness size

        // Vertical text grouping stack layer
        GameObject textContainer = new GameObject("TextStack", typeof(RectTransform));
        textContainer.transform.SetParent(rowObj.transform, false);
        RectTransform containerRt = textContainer.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(30, 0); // Shift clean off the border edge bounds
        containerRt.offsetMax = Vector2.zero;

        // Title Element string render
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(textContainer.transform, false);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = data.displayName;
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = Color.white;
        titleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 15);

        // Subtext Formula rendered right beneath it
        GameObject formulaObj = new GameObject("FormulaText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        formulaObj.transform.SetParent(textContainer.transform, false);
        TextMeshProUGUI formTxt = formulaObj.GetComponent<TextMeshProUGUI>();
        formTxt.text = data.formula;
        formTxt.fontSize = 15;
        formTxt.color = new Color(0.7f, 0.7f, 0.7f);
        formulaObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);

        // Functional Touch Logic assignment hook
        Button rowButton = rowObj.GetComponent<Button>();
        rowButton.onClick.AddListener(() => ExecuteRowSelection(data.formula, rowObj.GetComponent<Image>()));
    }

    private void CreateHeaderTextElement(string name, string label, int size, FontStyles style, Vector2 pos, Color? customColor = null)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(menuPanel.transform, false);
        
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = customColor ?? Color.white;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 45);
    }

    private void ExecuteRowSelection(string selectionID, Image rowBg)
    {
        Debug.Log($"Selected Chemical Target via Code: {selectionID}");

        // Reset all row background graphics to unselected states 
        foreach (Transform child in contentContainer)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.color = new Color(0.18f, 0.18f, 0.18f, 0.4f);
        }

        // Highlight active item using a dark teal tint theme matching your mockup
        rowBg.color = new Color(0.05f, 0.35f, 0.3f, 0.85f);

        // FIXED: Instead of assigning directly to the read-only property, we look for 
        // your original UI button objects by their name tags and invoke their click event.
        Button[] legacyButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool clickTriggered = false;

        foreach (Button oldBtn in legacyButtons)
        {
            // Verifies if the legacy gameobject button matches our formula target (e.g., "NaCl")
            if (oldBtn.gameObject.name.Trim().ToUpper() == selectionID.Trim().ToUpper())
            {
                oldBtn.onClick.Invoke(); // Simulates clicking your old button layout programmatically
                clickTriggered = true;
                break;
            }
        }

        if (!clickTriggered)
        {
            Debug.LogWarning($"UI Routing Redirect: Could not find an active or inactive legacy button named '{selectionID}' to trigger!");
        }
    }
}