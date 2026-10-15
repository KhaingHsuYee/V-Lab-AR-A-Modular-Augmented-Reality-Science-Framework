using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using System.Collections.Generic;

public struct OrganData
{
    public string nameEn, funcEn;
    public OrganData(string nEn, string fEn) 
    { 
        nameEn = nEn; funcEn = fEn; 
    }
}

public struct SystemData
{
    public string overviewEn;
    public List< OrganData > organs;
    public SystemData(string oEn, List< OrganData > orgs) 
    { 
        overviewEn = oEn; 
        organs = orgs; 
    }
}

public struct DetailedOrganData
{
    public string overview, function, location, clinical;
    public DetailedOrganData(string o, string f, string l, string c) 
    { 
        overview = o; function = f; location = l; clinical = c;
    }
}
public class Anatomy_MenuController : MonoBehaviour
{
    public UIDocument anatomyDocument;
    
    private VisualElement bottomSheet, infoPanel, organListContainer;
    private Button btnToggleMenu, btnPlayPause;
    private ScrollView systemView;
    private Label infoTitle, infoOverview, sheetTitle;
    
    private bool isMenuOpen = false;
    private bool isAutoRotating = true;
    public static bool isHoveringUI = false;
    private string currentActiveSystem = ""; 

    private bool isShowingSystems = true;
    private Button btnSysTab, btnOrgTab;

    private Dictionary< string, SystemData > anatomyDatabase = new Dictionary< string, SystemData >()
    {
        { "Skeletal", new SystemData(
            "Provides structural framework, protects organs, and anchors muscles.", 
            new List< OrganData > {
                new OrganData("Skull & Spine", "Protects the brain and spinal cord; supports posture."),
                new OrganData("Rib Cage & Pelvis", "Protects heart/lungs and bears the weight of the upper body."),
                new OrganData("Humerus, Radius & Ulna", "The primary bones of the upper arm and forearm."),
                new OrganData("Femur, Tibia & Fibula", "The weight-bearing bones of the thigh and lower leg.")
            })
        },
        { "Muscular", new SystemData(
            "Facilitates movement, maintains posture, and circulates blood.", 
            new List< OrganData > {
                new OrganData("Trapezius & Pectoralis", "Controls shoulder blade movement and arm flexion."),
                new OrganData("Deltoid, Biceps & Triceps", "Raises the arm and controls elbow flexion/extension."),
                new OrganData("Oblique", "Facilitates trunk rotation and stabilizes the core."),
                new OrganData("Quadriceps & Hamstrings", "Crucial for walking, jumping, and knee stabilization.")
            })
        },
        { "Nervous", new SystemData(
            "The body's control center, transmitting electrical signals.", 
            new List< OrganData > {
                new OrganData("Brain & Spinal Cord", "Processes sensory input and acts as the main neural highway."),
                new OrganData("Intercostal & Median Nerves", "Regulates breathing mechanics and forearm/hand control."),
                new OrganData("Sciatic, Femoral & Tibial Nerves", "Major peripheral nerves for leg movement and sensation.")
            })
        },
        { "Circulatory", new SystemData(
            "Pumps blood, transporting oxygen, nutrients, and hormones.", 
            new List< OrganData > {
                new OrganData("Heart & Aorta", "The central pump and the main artery distributing oxygenated blood."),
                new OrganData("Superior & Inferior Vena Cava", "Major veins returning deoxygenated blood to the heart."),
                new OrganData("Femoral Artery & Vein", "Primary blood supply and return for the lower limbs.")
            })
        },
        { "Respiratory", new SystemData(
            "Facilitates gas exchange (oxygen in, carbon dioxide out).", 
            new List< OrganData > {
                new OrganData("Nasal Cavity, Oral Cavity & Larynx", "Filters air and houses vocal cords."),
                new OrganData("Trachea & Lungs", "Directs air to the alveoli for gas exchange."),
                new OrganData("Diaphragm", "The primary muscle controlling inhalation.")
            })
        },
        { "Digestive", new SystemData(
            "Breaks down food to extract energy and nutrients.", 
            new List< OrganData > {
                new OrganData("Mouth & Esophagus", "Begins digestion and transports food to the stomach."),
                new OrganData("Stomach & Liver", "Breaks down food with acids and produces bile."),
                new OrganData("Small & Large Intestine", "Absorbs nutrients and water, forming solid waste."),
                new OrganData("Rectum", "Stores waste until elimination.")
            })
        },
        { "Endocrine", new SystemData(
            "Regulates metabolism, growth, and reproduction via hormones.", 
            new List< OrganData > {
                new OrganData("Thyroid Gland", "Regulates heart rate, blood pressure, and body temperature."),
                new OrganData("Thymus", "Crucial for the development of T-cells (immune system)."),
                new OrganData("Adrenal Gland", "Produces adrenaline, cortisol, and aldosterone."),
                new OrganData("Pancreas", "Regulates blood sugar levels via insulin and glucagon."),
                new OrganData("Ovary", "Produces reproductive hormones like estrogen and progesterone.")
            })
        },
        { "Urinary", new SystemData(
            "Filters blood, removes waste, and balances fluids.", 
            new List< OrganData > {
                new OrganData("Kidney", "Filters toxins from the blood and produces urine."),
                new OrganData("Ureter", "Tubes that carry urine from the kidneys to the bladder."),
                new OrganData("Bladder", "A muscular sac that stores urine."),
                new OrganData("Urethra", "The duct that transmits urine out of the body.")
            })
        },
        { "Lymphatic", new SystemData(
            "Maintains fluid levels and defends against infections.", 
            new List< OrganData > {
                new OrganData("Cervical, Axillary & Inguinal Lymph Nodes", "Filters lymph fluid in the neck, armpits, and groin."),
                new OrganData("Tonsils", "First line of defense against ingested or inhaled pathogens."),
                new OrganData("Thymus", "Site of T-cell maturation for the immune system."),
                new OrganData("Spleen", "Filters blood, recycling old red blood cells and storing white blood cells."),
                new OrganData("Popliteal Lymph Nodes", "Filters fluid from the lower legs and feet.")
            })
        }
    };

    private Dictionary< string, DetailedOrganData > organDatabase = new Dictionary< string, DetailedOrganData >()
    {
        { "Brain", new DetailedOrganData(
            "The most complex organ in the human body, acting as the central command center. It contains nearly 86 billion interconnected neurons.", 
            "Processes all sensory information, regulates involuntary functions like breathing, controls voluntary muscle movements, and is the physical seat of memory, emotion, and thought.", 
            "Safely encased within the cranial cavity of the skull, protected by cerebrospinal fluid.", 
            "Conditions like strokes, Alzheimer's, and Parkinson's disease directly impact its tissues.") },
            
        { "Diaphragm", new DetailedOrganData(
            "A thin, dome-shaped layer of muscle and tendon that plays a critical role in the respiratory system.", 
            "Contracts and flattens to create a vacuum that pulls air into the lungs. When it relaxes, air is forced out.", 
            "Sits at the base of the chest, separating the thoracic cavity (heart/lungs) from the abdominal cavity.", 
            "Involuntary spasms of this muscle are what cause humans to experience hiccups.") },
            
        { "Heart", new DetailedOrganData(
            "A powerful, continuously working four-chambered muscular pump that beats roughly 100,000 times a day.", 
            "Circulates oxygenated blood and essential nutrients throughout the entire body while routing deoxygenated blood to the lungs.", 
            "Located in the middle of the thoracic cavity, nested between the lungs and situated slightly left of the sternum.", 
            "Cardiovascular diseases, including arrhythmias and coronary artery disease, are major health concerns globally.") },
            
        { "Kidney", new DetailedOrganData(
            "A pair of bean-shaped filtering organs. Though small, they receive about 20% of the blood pumped by the heart.", 
            "Continuously filters toxins, excess water, and impurities from the blood to produce urine. They also regulate blood pressure and electrolytes.", 
            "Located against the back muscles in the upper abdominal area, one on each side of the spine.", 
            "Dehydration can lead to the crystallization of minerals, forming painful kidney stones.") },
            
        { "Liver", new DetailedOrganData(
            "The body's primary chemical processing plant and the largest solid internal organ, performing over 500 vital tasks.", 
            "Detoxifies harmful chemicals in the blood, secretes bile for fat digestion, stores vitamins, and synthesizes crucial proteins.", 
            "Fills the upper right quadrant of the abdomen, resting just beneath the diaphragm.", 
            "It is the only human organ capable of natural regeneration; it can regrow to a normal size even after 70% of it is removed.") },
            
        { "Lungs", new DetailedOrganData(
            "A pair of large, spongy, air-filled organs. The right lung has three lobes, while the left has two to make room for the heart.", 
            "Facilitates vital gas exchange: transferring oxygen from inhaled air into the bloodstream and expelling carbon dioxide.", 
            "Fills the majority of the rib cage within the thoracic cavity.", 
            "Conditions such as asthma, pneumonia, and chronic obstructive pulmonary disease (COPD) severely restrict their function.") },
            
        { "Pancreas", new DetailedOrganData(
            "A vital glandular organ that serves essential roles in both the digestive and endocrine systems.", 
            "Produces powerful digestive enzymes to break down food, and regulates systemic blood sugar levels by releasing insulin and glucagon.", 
            "Sits horizontally behind the stomach in the upper left abdomen.", 
            "Type 1 and Type 2 Diabetes occur when this organ either stops producing insulin or the body becomes resistant to it.") },
            
        { "Skull", new DetailedOrganData(
            "The complex bony framework of the head, composed of 22 individual bones that fuse together during early development.", 
            "Provides a rigid protective casing for the brain and forms the structural foundation for the face, jaws, and sensory organs.", 
            "Forms the topmost structure of the human skeletal system.", 
            "Unlike other bones, the mandible (jawbone) is the only highly mobile bone in this entire structure.") },
            
        { "Intestines", new DetailedOrganData(
            "A massive, continuous muscular tube comprising the small intestine (about 20 feet long) and the large intestine (about 5 feet long).", 
            "The small intestine absorbs the vast majority of nutrients, while the large intestine absorbs remaining water and forms solid waste.", 
            "Coiled extensively to fill the majority of the lower abdominal and pelvic cavities.", 
            "Issues like Irritable Bowel Syndrome (IBS), appendicitis, and Crohn's disease originate within this tract.") },
            
        { "Stomach", new DetailedOrganData(
            "A highly elastic, muscular hollow organ capable of expanding to hold up to a quart of food.", 
            "Uses churning muscle contractions mixed with powerful hydrochloric acid and pepsin to break down food into a liquid mixture called chyme.", 
            "Located on the left side of the upper abdomen, receiving food from the esophagus.", 
            "Excessive acid or bacterial infections (H. pylori) can wear away the mucosal lining, causing painful ulcers.") }
            
        
    };

    void OnEnable()
    {
        var root = anatomyDocument.rootVisualElement;
        
        btnToggleMenu = root.Q< Button >("btn-toggle-menu");
        btnToggleMenu.text = "EXPLORE ANATOMY";
        bottomSheet = root.Q< VisualElement >("bottom-sheet-panel");
        systemView = root.Q< ScrollView >("system-view");
        
        infoPanel = root.Q< VisualElement >("info-panel");
        infoTitle = root.Q< Label >("info-title");
        infoOverview = root.Q< Label >("info-overview");
        organListContainer = root.Q< VisualElement >("organ-list-container");
        sheetTitle = root.Q< Label >("sheet-title");
        
        btnPlayPause = root.Q< Button >("btn-play-pause");

        infoPanel.RegisterCallback< PointerEnterEvent >(e => isHoveringUI = true);
        infoPanel.RegisterCallback< PointerLeaveEvent >(e => isHoveringUI = false);
        bottomSheet.RegisterCallback< PointerEnterEvent >(e => isHoveringUI = true);
        bottomSheet.RegisterCallback< PointerLeaveEvent >(e => isHoveringUI = false);
        root.RegisterCallback< PointerUpEvent >(e => isHoveringUI = false); 

        btnToggleMenu.clicked += ToggleMenu;
        root.Q< Button >("btn-close-menu").clicked += ToggleMenu;
        root.Q< Button >("btn-close-info").clicked += () => infoPanel.style.display = DisplayStyle.None;

        if (btnPlayPause != null)
        {
            btnPlayPause.clicked += () => {
                isAutoRotating = !isAutoRotating;
                btnPlayPause.text = isAutoRotating ? "⏸" : "▶";
                FindAnyObjectByType< AR_AnatomySpawner >()?.SetAutoRotation(isAutoRotating);
            };
        }

        // Create the Tab Container
        VisualElement tabContainer = new VisualElement();
        tabContainer.style.flexDirection = FlexDirection.Row;
        tabContainer.style.justifyContent = Justify.Center;
        tabContainer.style.marginBottom = 10;
        tabContainer.style.marginTop = 10;

        btnSysTab = new Button { text = "SYSTEMS" };
        btnOrgTab = new Button { text = "ORGANS" };

        // Style the tabs
        btnSysTab.style.flexGrow = 1;
        btnOrgTab.style.flexGrow = 1;
        btnSysTab.style.height = 45;
        btnOrgTab.style.height = 45;
        btnSysTab.style.color = Color.white;
        btnOrgTab.style.color = Color.white;
        btnSysTab.style.fontSize = 14;
        btnOrgTab.style.fontSize = 14;
        btnSysTab.style.unityFontStyleAndWeight = FontStyle.Bold;
        btnOrgTab.style.unityFontStyleAndWeight = FontStyle.Bold;

        tabContainer.Add(btnSysTab);
        tabContainer.Add(btnOrgTab);

        // Insert the tabs into the UI (Index 1 is right below the sheet title)
        bottomSheet.Insert(1, tabContainer);

        // Add Click Events
        btnSysTab.clicked += () => { isShowingSystems = true; PopulateMenu(); };
        btnOrgTab.clicked += () => { isShowingSystems = false; PopulateMenu(); };

        PopulateMenu();
    }

    private void PopulateMenu()
    {
        systemView.Clear();

        // Update tab colors (Blue for active, Dark Grey for inactive)
        btnSysTab.style.backgroundColor = isShowingSystems ? new Color(0.1f, 0.5f, 0.9f) : new Color(0.2f, 0.2f, 0.2f);
        btnOrgTab.style.backgroundColor = !isShowingSystems ? new Color(0.1f, 0.5f, 0.9f) : new Color(0.2f, 0.2f, 0.2f);
        
        // Update the main title
        if (sheetTitle != null) 
        {
            sheetTitle.text = isShowingSystems ? "BIOLOGICAL SYSTEMS" : "INDIVIDUAL ORGANS";
        }

        if (isShowingSystems)
        {
            foreach (string sysName in anatomyDatabase.Keys)
            {
                Button btn = new Button { text = sysName };
                btn.AddToClassList("primary-btn"); // Uses your existing USS styling
                btn.clicked += () => OnSystemSelected(sysName);
                systemView.Add(btn);
            }
        }
        else
        {
            foreach (string orgName in organDatabase.Keys)
            {
                Button btn = new Button { text = orgName };
                btn.AddToClassList("primary-btn"); // Uses your existing USS styling
                btn.clicked += () => OnOrganSelected(orgName);
                systemView.Add(btn);
            }
        }
    }
    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        float targetY = isMenuOpen ? 0f : 150f;
        btnToggleMenu.style.display = isMenuOpen ? DisplayStyle.None : DisplayStyle.Flex;
        
        DOTween.To(() => bottomSheet.style.translate.value.y.value, 
            y => bottomSheet.style.translate = new StyleTranslate(new Translate(new Length(0, LengthUnit.Percent), new Length(y, LengthUnit.Percent))), 
            targetY, 0.4f).SetEase(isMenuOpen ? Ease.OutBack : Ease.InBack);
    }

    private void OnSystemSelected(string systemName)
    {
        ToggleMenu(); 
        FindAnyObjectByType< AR_AnatomySpawner >()?.StartTargeting(systemName);
    }

    public void ShowSystemInfo(string systemName)
    {
        if (!anatomyDatabase.ContainsKey(systemName)) return;

        currentActiveSystem = systemName;
        SystemData data = anatomyDatabase[systemName];
        
        infoTitle.text = systemName + " System";
        infoOverview.text = data.overviewEn;

        // Revert the UI headers back to System defaults
        var sectionTitles = infoPanel.Query< Label >(className: "section-title").ToList();
        if (sectionTitles.Count >= 2)
        {
            sectionTitles[0].text = "System Overview";
            sectionTitles[1].text = "Key Organs";
        }
        organListContainer.Clear();
        foreach (var organ in data.organs)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("organ-card");

            Label nameLabel = new Label(organ.nameEn);
            nameLabel.AddToClassList("organ-name");

            Label descLabel = new Label(organ.funcEn);
            descLabel.AddToClassList("body-text");

            card.Add(nameLabel);
            card.Add(descLabel);
            organListContainer.Add(card);
        }

        infoPanel.style.display = DisplayStyle.Flex;
    }

    private void OnOrganSelected(string organName)
    {
        ToggleMenu(); 
        FindAnyObjectByType< AR_AnatomySpawner >()?.StartTargeting(organName);
    }

    public void ShowOrganInfo(string organName)
    {
        if (!organDatabase.ContainsKey(organName)) return;

        DetailedOrganData data = organDatabase[organName];
        infoTitle.text = organName;
        infoOverview.text = data.overview;
        
        // Dynamically change the UI headers so they make sense for an Organ
        var sectionTitles = infoPanel.Query< Label >(className: "section-title").ToList();
        if (sectionTitles.Count >= 2)
        {
            sectionTitles[0].text = "Organ Overview";
            sectionTitles[1].text = "Detailed Information";
        }

        organListContainer.Clear(); 

        // Create the new detailed text blocks
        Label funcLabel = new Label("<b>Primary Function:</b>\n" + data.function);
        funcLabel.AddToClassList("body-text");
        funcLabel.style.whiteSpace = WhiteSpace.Normal;
        funcLabel.style.marginBottom = 10;

        Label locLabel = new Label("<b>Location:</b>\n" + data.location);
        locLabel.AddToClassList("body-text");
        locLabel.style.whiteSpace = WhiteSpace.Normal;
        locLabel.style.marginBottom = 10;

        Label clinLabel = new Label("<b>Clinical Notes:</b>\n" + data.clinical);
        clinLabel.AddToClassList("body-text");
        clinLabel.style.whiteSpace = WhiteSpace.Normal;
        clinLabel.style.marginBottom = 10;

        organListContainer.Add(funcLabel);
        organListContainer.Add(locLabel);
        organListContainer.Add(clinLabel);

        infoPanel.style.display = DisplayStyle.Flex;
    }
    
    void Start()
    {
        if (btnToggleMenu != null) btnToggleMenu.style.display = DisplayStyle.None;
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.None;
    }

    public void EnableMenuButton()
    {
        if (btnToggleMenu != null) btnToggleMenu.style.display = DisplayStyle.Flex;
    }
}