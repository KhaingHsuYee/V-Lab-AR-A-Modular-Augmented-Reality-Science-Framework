using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; 

public class DemonstrationMenuManager : MonoBehaviour
{
    [Header("UI Document Setup")]
    [SerializeField] private UIDocument demonstrationUIDocument;

    private void OnEnable()
    {
        // Make sure the UI Document is assigned and loaded
        if (demonstrationUIDocument != null && demonstrationUIDocument.rootVisualElement != null)
        {
            var root = demonstrationUIDocument.rootVisualElement;

            // --- 1. FIND AND CONNECT THE BACK BUTTON ---
            Button btnBack = root.Q<Button>("btn-back");
            if (btnBack != null) btnBack.clicked += OnBackButtonPressed;

            // --- 2. FIND AND CONNECT THE DEMONSTRATION BUTTONS ---
            Button btnPeriodic = root.Q<Button>("btn-periodic");
            if (btnPeriodic != null) btnPeriodic.clicked += () => LoadDemonstration("Periodic_Table");

            Button btnSolar = root.Q<Button>("btn-solar");
            if (btnSolar != null) btnSolar.clicked += () => LoadDemonstration("Solar_System");

            Button btnMolecule = root.Q<Button>("btn-molecule");
            if (btnMolecule != null) btnMolecule.clicked += () => LoadDemonstration("AR_Molecule");

            // --- 3. ADDED ANATOMY EXPLORER BUTTON ---
            Button btnAnatomy = root.Q<Button>("btn-anatomy");
            if (btnAnatomy != null) btnAnatomy.clicked += () => LoadDemonstration("Anatomy_Explorer");
        }
    }

    // --- HELPER METHODS TO LOAD SCENES ---

    private void LoadDemonstration(string sceneName)
    {
        Debug.Log($"Attempting to load scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void OnBackButtonPressed()
    {
        Debug.Log("Back button pressed! Returning to previous screen...");
        // Change "HomeScreen" to whatever your absolute first menu scene is called
        SceneManager.LoadScene("HomeScreen"); 
    }
}