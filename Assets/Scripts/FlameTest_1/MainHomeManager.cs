using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainHomeManager : MonoBehaviour
{
    [Header("UI Document Setup")]
    [SerializeField] private UIDocument homeUIDocument;

    private VisualElement leftPanel;
    private VisualElement cardChemistry;
    private VisualElement cardPhysics;
    private Button btnExit;
    
    // Added reference for the title animation
    private Label titleLabel;

    private Button btnAbout;
    private Button btnInfo;

    private void OnEnable()
    {
        if (homeUIDocument != null && homeUIDocument.rootVisualElement != null)
        {
            var root = homeUIDocument.rootVisualElement;

            // 1. Grab the elements we want to animate
            leftPanel = root.Q<VisualElement>("left-panel");
            cardChemistry = root.Q<VisualElement>("card-chemistry");
            cardPhysics = root.Q<VisualElement>("card-physics");
            btnExit = root.Q<Button>("btn-exit");
            
            // Grab the title label mapped in the UXML
            titleLabel = root.Q<Label>("vlab-title");

            // 2. Connect the buttons
            Button btnChemistry = root.Q<Button>("btn-chemistry");
            if (btnChemistry != null) btnChemistry.clicked += () => LoadScene("Chemistry_Home");

            Button btnPhysics = root.Q<Button>("btn-physics");
            if (btnPhysics != null) btnPhysics.clicked += () => LoadScene("Demonstration_Home");

            if (btnExit != null) btnExit.clicked += QuitApplication;

            // Inside OnEnable():
            btnAbout = root.Q<Button>("btn-about");
            if (btnAbout != null) btnAbout.clicked += () => LoadScene("AboutUs");

            btnInfo = root.Q<Button>("btn-info");
            if (btnInfo != null) btnInfo.clicked += () => LoadScene("Limitations");

            // Execute the looping C# animation for the glowing title
            if (titleLabel != null)
            {
                titleLabel.schedule.Execute(() => 
                {
                    titleLabel.ToggleInClassList("brand-title-pulse");
                }).Every(1000);
            }

            // 3. Start the entrance animation
            StartCoroutine(AnimateUIIn());
        }
    }

    private IEnumerator AnimateUIIn()
    {
        // Wait a tiny fraction of a second to ensure UI Toolkit has drawn the first transparent frame
        yield return new WaitForSeconds(0.1f);

        // By removing these classes, Unity's CSS transitions will automatically slide them into place!
        if (leftPanel != null) leftPanel.RemoveFromClassList("hidden-start-left");
        if (btnExit != null) btnExit.RemoveFromClassList("hidden-start-top");
        if (btnInfo != null) btnInfo.RemoveFromClassList("hidden-start-top");

        yield return new WaitForSeconds(0.1f); // Stagger the cards slightly
        if (cardChemistry != null) cardChemistry.RemoveFromClassList("hidden-start-right");
        
        yield return new WaitForSeconds(0.1f);
        if (cardPhysics != null) cardPhysics.RemoveFromClassList("hidden-start-right");

    }

    private void LoadScene(string sceneName)
    {
        Debug.Log($"Loading {sceneName}...");
        SceneManager.LoadScene(sceneName);
    }

    private void QuitApplication()
    {
        Debug.Log("Exiting Application...");
        Application.Quit();
    }
}