using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class KirchhoffLoop_ResultsManager : MonoBehaviour
{
    [SerializeField] private UIDocument resultsUIDocument;
    
    private const float AllowedErrorMargin = 0.05f;  

    private VisualElement resultsOverlay; 
    private VisualElement resultsPanel;   
    private VisualElement successPopup;

    private TextField inputV1;
    private TextField inputV2;
    private TextField inputV3;
    private TextField inputVTotal;

    public bool IsUIOpen = false;

    void Start()
    {
        if (resultsUIDocument == null) return;
        var root = resultsUIDocument.rootVisualElement;

        resultsOverlay = root.Q<VisualElement>("results-overlay");
        resultsPanel = root.Q<VisualElement>("results-panel");
        successPopup = root.Q<VisualElement>("success-popup");

        inputV1 = root.Q<TextField>("input-v1");
        inputV2 = root.Q<TextField>("input-v2");
        inputV3 = root.Q<TextField>("input-v3");
        inputVTotal = root.Q<TextField>("input-vtotal");

        Button gradeBtn = root.Q<Button>("btn-grade");
        if (gradeBtn != null) gradeBtn.clicked += GradeExperiment;

        Button closeBtn = root.Q<Button>("btn-close-panel");
        if (closeBtn != null) closeBtn.clicked += ToggleResultsPanel;

        Button restartBtn = root.Q<Button>("btn-restart-lab");
        if (restartBtn != null) restartBtn.clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        Button exitBtn = root.Q<Button>("btn-exit-lab");
        if (exitBtn != null) exitBtn.clicked += () => SceneManager.LoadScene("Chemistry_Home"); 
    }

    private void GradeExperiment()
    {
        if (float.TryParse(inputV1.value, out float v1) && 
            float.TryParse(inputV2.value, out float v2) && 
            float.TryParse(inputV3.value, out float v3) &&
            float.TryParse(inputVTotal.value, out float vTotal))
        {
            float expectedTotal = v1 + v2 + v3;
            float difference = Mathf.Abs(vTotal - expectedTotal);
            
            if (vTotal > 0.1f && difference <= (expectedTotal * AllowedErrorMargin))
            {
                inputV1.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 
                inputV2.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 
                inputV3.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 
                inputVTotal.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 

                if (successPopup != null)
                {
                    successPopup.style.display = DisplayStyle.Flex;
                    successPopup.style.scale = new StyleScale(new Vector2(0, 0));
                    DOTween.To(() => Vector2.zero, x => successPopup.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBounce);
                }
            }
            else
            {
                inputVTotal.style.backgroundColor = new StyleColor(new Color(1, 0, 0, 0.2f));
            }
        }
    }

    public void ToggleResultsPanel()
    {
        if (resultsOverlay == null || resultsPanel == null) return;

        if (!IsUIOpen)
        {
            IsUIOpen = true;
            resultsOverlay.style.display = DisplayStyle.Flex;
            resultsPanel.style.scale = new StyleScale(new Vector2(0, 0));
            DOTween.To(() => Vector2.zero, x => resultsPanel.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            IsUIOpen = false;
            DOTween.To(() => Vector2.one, x => resultsPanel.style.scale = new StyleScale(x), Vector2.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                resultsOverlay.style.display = DisplayStyle.None;
            });
        }
    }
}