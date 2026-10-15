using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using System.Collections.Generic;

// 1. Data structure to hold the facts for ANY model
public struct MoleculeData
{
    public string formula;
    public string description;
    public string stats;

    public MoleculeData(string form, string desc, string st)
    {
        formula = form;
        description = desc;
        stats = st;
    }
}

public class Molecular_MenuController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument bottomSheetDocument;
    
    private VisualElement bottomSheetPanel;
    private Button btnToggleMenu;
    private Button btnBack;
    private Label sheetTitle;
    private VisualElement categoryView;
    private ScrollView itemView;
    private VisualElement legendPanel;

    // --- INFO PANEL VARIABLES ---
    private VisualElement infoPanel;
    private Label infoTitle;
    private Label infoFormula;
    private Label infoDesc;
    private Label infoStats;
    private Button btnCloseInfo;
    // --------------------------------

    private bool isMenuOpen = false;
    private float menuYPercent = 150f; 

    // Menu Categories (Restored to your full list)
    private Dictionary<string, List<string>> moleculeDatabase = new Dictionary<string, List<string>>()
    {
        { "HYDROCARBONS", new List<string> { "Methane", "Ethane", "Propane", "Butane", "Hexane", "Decane", "Cyclopropane", "Cyclooctane", "Acetylene" } },
        { "MEDICINE CABINET", new List<string> { "Aspirin", "Acetaminophen", "Ibuprofen", "Penicillin", "Thalidomide" } },
        { "NEUROTRANSMITTERS", new List<string> { "Dopamine", "Serotonin", "Adrenaline", "Melatonin", "Histamine", "Acetylcholine", "Cortisol" } },
        { "SCENTS & FLAVORS", new List<string> { "Caffeine", "Vanillin", "Cinnamaldehyde", "Eugenol", "Glucose", "Xylitol" } }
    };

    // 2. Highly detailed dictionary with proper Unicode subscripts
    private Dictionary<string, MoleculeData> moleculeInfoData = new Dictionary<string, MoleculeData>()
    {
        // --- HYDROCARBONS ---
        { "Methane", new MoleculeData("CH₄", "The simplest hydrocarbon and main component of natural gas. It is a colorless, odorless gas highly reactive with halogens.", "Molar Mass: 16.04 g/mol\nDensity: 0.656 kg/m³\nBoiling Point: -161.5°C\nSolubility: Soluble in ethanol") },
        { "Ethane", new MoleculeData("C₂H₆", "A colorless, odorless gas isolated on an industrial scale from natural gas and used widely to produce ethylene for plastics.", "Molar Mass: 30.07 g/mol\nDensity: 1.356 kg/m³\nBoiling Point: -89.0°C\nSolubility: Moderately soluble in water") },
        { "Propane", new MoleculeData("C₃H₈", "A three-carbon alkane commonly compressed and used as fuel for engines, barbecues, and portable heating stoves.", "Molar Mass: 44.10 g/mol\nDensity: 1.88 kg/m³\nBoiling Point: -42.0°C\nAppearance: Colorless gas") },
        { "Butane", new MoleculeData("C₄H₁₀", "A highly flammable, colorless gas at room temperature. It is commonly used as a propellant in aerosols and fuel in lighters.", "Molar Mass: 58.12 g/mol\nDensity: 2.48 kg/m³\nBoiling Point: -1.0°C\nState: Liquefies easily under pressure") },
        { "Hexane", new MoleculeData("C₆H₁₄", "A liquid alkane widely used as a cheap, relatively safe, largely unreactive, and easily evaporated non-polar solvent.", "Molar Mass: 86.18 g/mol\nDensity: 0.659 g/cm³\nBoiling Point: 69.0°C\nSolubility: Insoluble in water") },
        { "Decane", new MoleculeData("C₁₀H₂₂", "A liquid alkane that is a common component of gasoline (petrol) and kerosene. Like other alkanes, it is non-polar.", "Molar Mass: 142.29 g/mol\nDensity: 0.730 g/cm³\nBoiling Point: 174.1°C\nAppearance: Colorless liquid") },
        { "Cyclopropane", new MoleculeData("C₃H₆", "A cycloalkane molecule with three carbon atoms forming a ring. Because of ring strain, it is much more reactive than open-chain alkanes.", "Molar Mass: 42.08 g/mol\nDensity: 1.879 kg/m³\nBoiling Point: -33.0°C\nHistorical Use: Anesthetic") },
        { "Cyclooctane", new MoleculeData("C₈H₁₆", "A cycloalkane used as an intermediate in the production of plastics, synthetic fibers, and adhesives.", "Molar Mass: 112.21 g/mol\nDensity: 0.834 g/cm³\nBoiling Point: 149.0°C\nAppearance: Colorless liquid") },
        { "Acetylene", new MoleculeData("C₂H₂", "The simplest alkyne, containing a carbon-carbon triple bond. It is widely used as a fuel and a chemical building block.", "Molar Mass: 26.04 g/mol\nDensity: 1.097 kg/m³\nSublimation Point: -84.0°C\nFlame Temp: 3,150°C") },

        // --- MEDICINE CABINET ---
        { "Aspirin", new MoleculeData("C₉H₈O₄", "Also known as acetylsalicylic acid, it is a nonsteroidal anti-inflammatory drug (NSAID) used to reduce pain, fever, or inflammation.", "Molar Mass: 180.16 g/mol\nDensity: 1.40 g/cm³\nMelting Point: 136.0°C\nSolubility: Soluble in ethanol and ether") },
        { "Acetaminophen", new MoleculeData("C₈H₉NO₂", "Also known as paracetamol, it is a widely used medication to treat mild to moderate pain and reduce fevers.", "Molar Mass: 151.16 g/mol\nDensity: 1.29 g/cm³\nMelting Point: 169.0°C\nAppearance: White crystalline solid") },
        { "Ibuprofen", new MoleculeData("C₁₃H₁₈O₂", "A common NSAID used for treating pain, fever, and inflammation, commonly sold under brand names like Advil and Motrin.", "Molar Mass: 206.29 g/mol\nDensity: 1.03 g/cm³\nMelting Point: 76.0°C\nSolubility: Practically insoluble in water") },
        { "Penicillin", new MoleculeData("C₁₆H₁₈N₂O₄S", "A group of antibacterial drugs originally derived from Penicillium fungi. (Data represents Penicillin G).", "Molar Mass: 334.39 g/mol\nDiscovery: 1928 by Alexander Fleming\nClassification: Beta-lactam antibiotic") },
        { "Thalidomide", new MoleculeData("C₁₃H₁₀N₂O₄", "An immunomodulatory drug that caused historical birth defects in the 1950s, but is now highly regulated and used to treat leprosy and myeloma.", "Molar Mass: 258.23 g/mol\nMelting Point: 271.0°C\nAppearance: White crystalline powder") },

        // --- NEUROTRANSMITTERS ---
        { "Dopamine", new MoleculeData("C₈H₁₁NO₂", "A neurotransmitter heavily involved in the brain's reward system, motivation, and motor control. It is synthesized in the brain and kidneys.", "Molar Mass: 153.18 g/mol\nMelting Point: 128.0°C\nClassification: Catecholamine") },
        { "Serotonin", new MoleculeData("C₁₀H₁₂N₂O", "A complex monoamine neurotransmitter that modulates mood, cognition, reward, learning, memory, and numerous physiological processes.", "Molar Mass: 176.21 g/mol\nMelting Point: 121.0°C\nSynthesis: Derived from tryptophan") },
        { "Adrenaline", new MoleculeData("C₉H₁₃NO₃", "Also known as epinephrine, a hormone and neurotransmitter that triggers the body's 'fight-or-flight' response, increasing heart rate and blood flow.", "Molar Mass: 183.20 g/mol\nMelting Point: 212.0°C\nProduction: Adrenal glands") },
        { "Melatonin", new MoleculeData("C₁₃H₁₆N₂O₂", "A hormone primarily released by the pineal gland at night that regulates the sleep-wake cycle (circadian rhythm).", "Molar Mass: 232.28 g/mol\nMelting Point: 117.0°C\nAppearance: Pale yellow crystalline powder") },
        { "Histamine", new MoleculeData("C₅H₉N₃", "An organic nitrogenous compound involved in local immune responses and regulating physiological function in the gut.", "Molar Mass: 111.15 g/mol\nMelting Point: 84.0°C\nBoiling Point: 167.0°C\nFunction: Inflammatory mediator") },
        { "Acetylcholine", new MoleculeData("C₇H₁₆NO₂", "The chief neurotransmitter of the parasympathetic nervous system, responsible for contracting smooth muscles and dilating blood vessels.", "Molar Mass: 146.21 g/mol\nReceptor types: Nicotinic & Muscarinic\nFunction: Motor neuron communication") },
        { "Cortisol", new MoleculeData("C₂₁H₃₀O₅", "A steroid hormone that regulates a wide range of processes throughout the body, including metabolism and the immune response to stress.", "Molar Mass: 362.46 g/mol\nMelting Point: 212.0°C\nClass: Glucocorticoid") },

        // --- SCENTS & FLAVORS ---
        { "Caffeine", new MoleculeData("C₈H₁₀N₄O₂", "A central nervous system stimulant of the methylxanthine class. It reversibly blocks the action of adenosine on its receptors.", "Molar Mass: 194.19 g/mol\nDensity: 1.23 g/cm³\nMelting Point: 235.0°C\nAppearance: Odorless, white needles") },
        { "Vanillin", new MoleculeData("C₈H₈O₃", "A phenolic aldehyde containing ether and alcohol functional groups. It is the primary chemical component of vanilla bean extract.", "Molar Mass: 152.15 g/mol\nDensity: 1.056 g/cm³\nMelting Point: 81.0°C\nOdor: Pleasant, sweet, vanilla-like") },
        { "Cinnamaldehyde", new MoleculeData("C₉H₈O", "An organic compound consisting of a phenyl group attached to an unsaturated aldehyde. It gives cinnamon its distinct flavor and odor.", "Molar Mass: 132.16 g/mol\nDensity: 1.05 g/cm³\nBoiling Point: 248.0°C\nAppearance: Yellow viscous liquid") },
        { "Eugenol", new MoleculeData("C₁₀H₁₂O₂", "An allyl chain-substituted guaiacol. It is a colorless to pale yellow oily liquid extracted from certain essential oils like clove and nutmeg.", "Molar Mass: 164.20 g/mol\nDensity: 1.06 g/cm³\nBoiling Point: 254.0°C\nOdor: Spicy, clove-like") },
        { "Glucose", new MoleculeData("C₆H₁₂O₆", "A simple sugar (monosaccharide). It is the most abundant monosaccharide in nature and a primary energy source for biological organisms.", "Molar Mass: 180.16 g/mol\nDensity: 1.54 g/cm³\nMelting Point: 146.0°C\nSolubility: Highly soluble in water") },
        { "Xylitol", new MoleculeData("C₅H₁₂O₅", "A polyalcohol (sugar alcohol) commonly used as a sugar substitute in products like chewing gum, mints, and toothpaste.", "Molar Mass: 152.15 g/mol\nDensity: 1.52 g/cm³\nMelting Point: 92.0°C\nSafety Note: Highly toxic to dogs") }
    };

    void OnEnable()
    {
        var root = bottomSheetDocument.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;

        bottomSheetPanel = root.Q<VisualElement>("bottom-sheet-panel");
        btnToggleMenu = root.Q<Button>("btn-toggle-menu");
        btnBack = root.Q<Button>("btn-back");
        sheetTitle = root.Q<Label>("sheet-title");
        categoryView = root.Q<VisualElement>("category-view");
        itemView = root.Q<ScrollView>("item-view");
        legendPanel = root.Q<VisualElement>("legend-panel");

        // --- BIND INFO PANEL UI ---
        infoPanel = root.Q<VisualElement>("info-panel");
        infoTitle = root.Q<Label>("info-title");
        infoFormula = root.Q<Label>("info-formula");
        infoDesc = root.Q<Label>("info-desc");
        infoStats = root.Q<Label>("info-stats");
        btnCloseInfo = root.Q<Button>("btn-close-info");

        if (btnCloseInfo != null)
        {
            btnCloseInfo.clicked += () => infoPanel.style.display = DisplayStyle.None;
        }

        btnToggleMenu.clicked += ToggleMenu;
        btnBack.clicked += ShowCategories;

        root.Q<Button>("cat-hydrocarbons").clicked += () => ShowItems("HYDROCARBONS");
        root.Q<Button>("cat-medicine").clicked += () => ShowItems("MEDICINE CABINET");
        root.Q<Button>("cat-neuro").clicked += () => ShowItems("NEUROTRANSMITTERS");
        root.Q<Button>("cat-scents").clicked += () => ShowItems("SCENTS & FLAVORS");
    }

    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (isMenuOpen)
        {
            btnToggleMenu.style.display = DisplayStyle.None;
            
            DOTween.To(() => menuYPercent, x => 
            {
                menuYPercent = x;
                bottomSheetPanel.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(x, LengthUnit.Percent)));
            }, 0f, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            CloseMenu();
        }
    }

    private void CloseMenu()
    {
        isMenuOpen = false;
        
        DOTween.To(() => menuYPercent, x => 
        {
            menuYPercent = x;
            bottomSheetPanel.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(x, LengthUnit.Percent)));
        }, 150f, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
            btnToggleMenu.style.display = DisplayStyle.Flex;
            ShowCategories(); 
        });
    }

    private void ShowCategories()
    {
        categoryView.style.display = DisplayStyle.Flex;
        itemView.style.display = DisplayStyle.None;
        btnBack.style.display = DisplayStyle.None;
        sheetTitle.text = "SELECT CATEGORY";
    }

    private void ShowItems(string categoryName)
    {
        categoryView.style.display = DisplayStyle.None;
        itemView.style.display = DisplayStyle.Flex;
        btnBack.style.display = DisplayStyle.Flex;
        sheetTitle.text = categoryName;

        itemView.Clear();

        if (moleculeDatabase.ContainsKey(categoryName))
        {
            foreach (string mol in moleculeDatabase[categoryName])
            {
                Button newMoleculeBtn = new Button();
                newMoleculeBtn.text = mol;
                newMoleculeBtn.AddToClassList("item-card"); 
                
                newMoleculeBtn.clicked += () => OnMoleculeSelected(mol);
                
                itemView.Add(newMoleculeBtn);
            }
        }
    }

    private void OnMoleculeSelected(string moleculeName)
    {
        CloseMenu();

        // Tell the Spawner to start the targeting phase
        Object.FindFirstObjectByType<AR_MoleculeSpawner>().StartTargeting(moleculeName);
    }

    // Called by the AR Spawner after placement
    public void ShowMoleculeInfo(string moleculeName)
    {
        if (moleculeInfoData.ContainsKey(moleculeName))
        {
            MoleculeData data = moleculeInfoData[moleculeName];
            
            infoTitle.text = moleculeName;
            infoFormula.text = data.formula;
            infoDesc.text = data.description;
            infoStats.text = data.stats;

            infoPanel.style.display = DisplayStyle.Flex;
        }
        else
        {
            infoPanel.style.display = DisplayStyle.None; 
        }
    }

    void Start()
    {
        if (btnToggleMenu != null) btnToggleMenu.style.display = DisplayStyle.None;
        if (legendPanel != null) legendPanel.style.display = DisplayStyle.None;
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.None;
    }

    public void EnableMenuButton()
    {
        if (btnToggleMenu != null) btnToggleMenu.style.display = DisplayStyle.Flex;
        if (legendPanel != null) legendPanel.style.display = DisplayStyle.Flex;
    }
}