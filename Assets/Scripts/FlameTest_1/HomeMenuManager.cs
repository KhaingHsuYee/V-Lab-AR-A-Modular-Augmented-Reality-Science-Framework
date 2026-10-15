using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; 

public class HomeMenuManager : MonoBehaviour
{
    [Header("UI Document Setup")]
    [SerializeField] private UIDocument homeUIDocument;

    private void OnEnable()
    {
        if (homeUIDocument != null && homeUIDocument.rootVisualElement != null)
        {
            var root = homeUIDocument.rootVisualElement;

            Button btnBack = root.Q<Button>("btn-back");
            if (btnBack != null) btnBack.clicked += OnBackButtonPressed;

            Button btnFlame = root.Q<Button>("btn-flame");
            if (btnFlame != null) btnFlame.clicked += () => LoadExperiment("FlameTest_1"); 

            Button btnLead = root.Q<Button>("btn-lead");
            if (btnLead != null) btnLead.clicked += () => LoadExperiment("Basic_Radical_Lead");

            Button btnCopper = root.Q<Button>("btn-copper");
            if (btnCopper != null) btnCopper.clicked += () => LoadExperiment("Basic_Radical_Copper");

            Button btnFerrous = root.Q<Button>("btn-ferrous");
            if (btnFerrous != null) btnFerrous.clicked += () => LoadExperiment("Basic_Radical_Ferrous");

            Button btnVinegar = root.Q<Button>("btn-vinegar");
            if (btnVinegar != null) btnVinegar.clicked += () => LoadExperiment("Titration_Vinegar");

            Button btnTitration = root.Q<Button>("btn-titration");
            if (btnTitration != null) btnTitration.clicked += () => LoadExperiment("Titration_1");

            // --- NEW PHYSICS BUTTON BINDINGS ---
            Button btnOhm = root.Q<Button>("btn-ohm");
            if (btnOhm != null) btnOhm.clicked += () => LoadExperiment("Ohm_law"); // Update with actual scene name

            Button btnKirchhoffVoltage = root.Q<Button>("btn-kirchhoff-voltage");
            if (btnKirchhoffVoltage != null) btnKirchhoffVoltage.clicked += () => LoadExperiment("Kirchhoff_law Voltage"); // Update with actual scene name
        }
    }

    private void LoadExperiment(string sceneName)
    {
        Debug.Log($"Attempting to load scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void OnBackButtonPressed()
    {
        Debug.Log("Back button pressed! Returning to previous screen...");
        SceneManager.LoadScene("HomeScreen"); 
    }
}