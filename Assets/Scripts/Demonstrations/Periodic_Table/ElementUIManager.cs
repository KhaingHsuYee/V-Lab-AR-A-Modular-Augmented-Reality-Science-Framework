using UnityEngine;
using UnityEngine.UIElements;

public class ElementUIManager : MonoBehaviour
{
    public static ElementUIManager Instance;
    
    private VisualElement screenRoot;
    private Label symbolText, nameText, numberMassText, categoryText, descText, configText, electroText, radiusText, phaseText, densityText, meltText,boilText;
    private Label protonsText, neutronsText, electronsText;
    
    private Button btnPlayPause;
    private bool isAnimationPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        
        var root = GetComponent<UIDocument>().rootVisualElement;
        screenRoot = root.Q<VisualElement>("screen-root");

        symbolText = root.Q<Label>("symbol-text");
        nameText = root.Q<Label>("name-text");
        numberMassText = root.Q<Label>("number-mass-text");
        categoryText = root.Q<Label>("category-text");
        descText = root.Q<Label>("desc-text");
        configText = root.Q<Label>("config-text");
        electroText = root.Q<Label>("electro-text");
        radiusText = root.Q<Label>("radius-text");
        phaseText = root.Q<Label>("phase-text");
        densityText = root.Q<Label>("density-text");
        meltText = root.Q<Label>("melt-text");
        boilText = root.Q<Label>("boil-text");

        protonsText = root.Q<Label>("protons-text");
        neutronsText = root.Q<Label>("neutrons-text");
        electronsText = root.Q<Label>("electrons-text");

        btnPlayPause = root.Q<Button>("btn-play-pause");
        if (btnPlayPause != null) btnPlayPause.clicked += ToggleAnimation;

        screenRoot.style.display = DisplayStyle.None;
    }

    public void UpdateUI(ElementData_Periodic data)
    {
        if (screenRoot == null) return;
        screenRoot.style.display = DisplayStyle.Flex; 

        // Dynamic Colors
        Color catColor = Color.white; 
        string cat = data.category.ToLower();
        if (cat.Contains("nonmetal") || cat.Contains("halogen") || cat.Contains("noble")) catColor = new Color(0.0f, 0.8f, 1.0f); 
        else if (cat.Contains("alkali") || cat.Contains("earth")) catColor = new Color(1.0f, 0.5f, 0.2f); 
        else if (cat.Contains("transition") || cat.Contains("post-transition") || cat.Contains("metal")) catColor = new Color(0.9f, 0.8f, 0.3f); 
        else if (cat.Contains("metalloid")) catColor = new Color(0.4f, 0.9f, 0.4f); 
        else if (cat.Contains("actinide") || cat.Contains("lanthanide")) catColor = new Color(0.8f, 0.4f, 0.9f); 

        if (symbolText != null) { symbolText.text = data.symbol; symbolText.style.color = catColor; }
        if (categoryText != null) { categoryText.text = data.category; categoryText.style.color = catColor; }

        if (nameText != null) nameText.text = data.elementName;
        if (numberMassText != null) numberMassText.text = $"Atomic No. {data.atomicNumber}  |  {data.atomicWeight} u";
        
        if (descText != null) descText.text = string.IsNullOrEmpty(data.description) ? "No description available." : data.description;
        if (configText != null) configText.text = data.electronConfiguration;
        if (electroText != null) electroText.text = data.electronegativity.ToString();
        if (radiusText != null) radiusText.text = $"{data.atomicRadius} pm";
        if (phaseText != null) phaseText.text = data.phaseAtSTP;
        if (densityText != null) 
        {
            densityText.text = data.density == 0 ? "Unknown" : $"{data.density} g/cm³";
        }
        if (meltText != null) 
        {
            meltText.text = data.meltingPoint == 0 ? "Unknown" : $"{data.meltingPoint} K";
        }
        
        if (boilText != null) 
        {
            boilText.text = data.boilingPoint == 0 ? "Unknown" : $"{data.boilingPoint} K";
        }

        if (protonsText != null) protonsText.text = data.protons.ToString();
        if (neutronsText != null) neutronsText.text = data.neutrons.ToString();
        
        int totalElectrons = 0;
        if (data.electronShells != null) foreach (int shellCount in data.electronShells) totalElectrons += shellCount;
        if (electronsText != null) electronsText.text = totalElectrons.ToString();
    }

    private void ToggleAnimation()
    {
        isAnimationPaused = !isAnimationPaused;
        if (isAnimationPaused)
        {
            if (btnPlayPause != null) btnPlayPause.text = "▶";
            Time.timeScale = 0f; 
        }
        else
        {
            if (btnPlayPause != null) btnPlayPause.text = "⏸";
            Time.timeScale = 1f; 
        }
    }

    public void HideUI()
    {
        if (screenRoot != null) screenRoot.style.display = DisplayStyle.None;
        if (isAnimationPaused) ToggleAnimation(); 
    }
}