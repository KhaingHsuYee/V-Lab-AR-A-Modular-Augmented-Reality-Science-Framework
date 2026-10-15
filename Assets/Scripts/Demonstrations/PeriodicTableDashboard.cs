using UnityEngine;
using UnityEngine.UIElements;

public class PeriodicTableDashboard : MonoBehaviour
{
    [Header("AR Tracking")]
    [Tooltip("The spawned Periodic Table GameObject to hover above.")]
    public Transform targetTable;
    public float heightOffset = 0.6f; // How high above the table the UI hovers

    private UIDocument uiDocument;

    // UI Elements
    private VisualElement root;
    private TextField searchInput;
    private Button btnSearch;
    
    // Data Labels
    private Label txtName, txtSubtitle, txtFact, txtHistory, txtAppearance, txtState;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        if (root == null) return;

        // 1. Get references to the right-side Data Panel labels
        txtName = root.Q<Label>("txt-element-name");
        txtSubtitle = root.Q<Label>("txt-element-subtitle");
        txtFact = root.Q<Label>("txt-fact");
        txtHistory = root.Q<Label>("txt-history");
        txtAppearance = root.Q<Label>("txt-appearance");
        txtState = root.Q<Label>("txt-state");

        // 2. Setup the Search Bar
        searchInput = root.Q<TextField>("search-input");
        btnSearch = root.Q<Button>("btn-search");
        
        if (btnSearch != null)
        {
            btnSearch.clicked += OnSearchClicked;
        }

        // 3. Setup the Legend Category Buttons
        BindCategoryButton("btn-group-alkali", "Na");       // Sodium for Alkali example
        BindCategoryButton("btn-group-transition", "Cu");   // Copper for Transition example
        BindCategoryButton("btn-group-halogen", "Cl");      // Chlorine for Halogen example
        BindCategoryButton("btn-group-noble", "Ne");        // Neon for Noble Gas example
        BindCategoryButton("btn-group-lanthanide", "La");   // Lanthanum for Lanthanide example
        BindCategoryButton("btn-group-actinide", "U");      // Uranium for Actinide example
        BindCategoryButton("btn-group-nonmetal", "C");      // Carbon for Nonmetal example
        
        // Hide dashboard initially until table is spawned
        root.style.display = DisplayStyle.None; 
    }

    private void BindCategoryButton(string btnName, string elementSymbolToLoad)
    {
        Button btn = root.Q<Button>(btnName);
        if (btn != null)
        {
            btn.clicked += () => LoadElementData(elementSymbolToLoad);
        }
    }

    private void OnSearchClicked()
    {
        if (searchInput != null && !string.IsNullOrEmpty(searchInput.value))
        {
            LoadElementData(searchInput.value);
        }
    }

    // Similar to your Populate(ElementData data) method from the Flame Test
    public void LoadElementData(string symbol)
    {
        ChemicalElementData data = ElementDatabase.GetElementBySymbol(symbol);

        if (data != null)
        {
            txtName.text = $"{data.Name} ({data.Symbol})";
            txtSubtitle.text = $"Atomic No. {data.AtomicNumber} | {data.Group}";
            txtFact.text = data.Fact;
            txtHistory.text = data.History;
            txtAppearance.text = data.Appearance;
            txtState.text = data.StateOfMatter;
        }
        else
        {
            txtName.text = "Element Not Found";
            txtSubtitle.text = "Check your spelling and try again.";
            txtFact.text = ""; txtHistory.text = ""; txtAppearance.text = ""; txtState.text = "";
        }
    }

    void Update()
    {
        // Billboarding & Tracking Logic
        if (targetTable != null && Camera.main != null)
        {
            // Make sure UI is visible once we have a target table
            if (root.style.display == DisplayStyle.None) root.style.display = DisplayStyle.Flex;

            // 1. Follow the table's position, but add height so it hovers above it
            transform.position = targetTable.position + (Vector3.up * heightOffset);

            // 2. The Fix: Look at the camera WITHOUT tilting (using your Flame Test math)
            Vector3 targetPosition = new Vector3(
                Camera.main.transform.position.x, 
                transform.position.y, // This locks the Y-axis so it doesn't slant
                Camera.main.transform.position.z
            );
            
            transform.LookAt(targetPosition);
            transform.Rotate(0, 180, 0); 
        }
    }
}