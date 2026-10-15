using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class AboutUsManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private UIDocument aboutUIDocument;

    private Button btnBack;
    private VisualElement glassCard;
    private Label titleLabel;

    private void OnEnable()
    {
        // Safety check to prevent NullReferenceExceptions
        if (aboutUIDocument == null || aboutUIDocument.rootVisualElement == null)
        {
            Debug.LogError("UIDocument is missing or the UXML Source Asset is empty!");
            return; 
        }

        var root = aboutUIDocument.rootVisualElement;
        
        btnBack = root.Q<Button>("btn-back");
        glassCard = root.Q<VisualElement>(className: "glass-card");
        titleLabel = root.Q<Label>("about-title");

        if (btnBack != null)
        {
            // Make sure this matches your actual main menu scene name exactly
            btnBack.clicked += () => SceneManager.LoadScene("HomeScreen"); 
        }

        if (titleLabel != null)
        {
            titleLabel.schedule.Execute(() => 
            {
                titleLabel.ToggleInClassList("brand-title-pulse");
            }).Every(1000);
        }

        StartCoroutine(AnimateUIIn());
    }

    private IEnumerator AnimateUIIn()
    {
        yield return new WaitForSeconds(0.1f);

        if (glassCard != null) glassCard.RemoveFromClassList("hidden-start-bottom");
        
        yield return new WaitForSeconds(0.15f);
        if (btnBack != null) btnBack.RemoveFromClassList("hidden-start-top");
    }
}