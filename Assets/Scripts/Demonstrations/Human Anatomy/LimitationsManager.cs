using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LimitationsManager : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private UIDocument limitationsUIDocument;

    private Button btnBack;
    private VisualElement glassCard;
    private Label titleLabel;

    private void OnEnable()
    {
        if (limitationsUIDocument == null || limitationsUIDocument.rootVisualElement == null) return;

        var root = limitationsUIDocument.rootVisualElement;
        
        btnBack = root.Q<Button>("btn-back");
        glassCard = root.Q<VisualElement>(className: "glass-card");
        titleLabel = root.Q<Label>("page-title");

        if (btnBack != null)
        {
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