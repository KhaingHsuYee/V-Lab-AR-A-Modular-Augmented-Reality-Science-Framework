using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class OhmLaw_ResultsManager : MonoBehaviour
{
    [SerializeField] private UIDocument resultsUIDocument;
    
    private const float TargetResistor = 50.0f;
    private const float AllowedErrorMargin = 0.05f;  

    private VisualElement root;
    private VisualElement resultsOverlay; 
    private VisualElement resultsPanel;   
    private VisualElement successPopup;
    private VisualElement graphContainer;

    public bool IsUIOpen = false;

    void Start()
    {
        if (resultsUIDocument == null) return;
        root = resultsUIDocument.rootVisualElement;

        resultsOverlay = root.Q<VisualElement>("results-overlay");
        resultsPanel = root.Q<VisualElement>("results-panel");
        successPopup = root.Q<VisualElement>("success-popup");
        graphContainer = root.Q<VisualElement>("graph-container");

        for (int i = 1; i <= 5; i++)
        {
            SetupRowAutoCalculation(i);
        }

        Button gradeBtn = root.Q<Button>("btn-grade");
        if (gradeBtn != null) gradeBtn.clicked += GradeAllRows;

        Button closeBtn = root.Q<Button>("btn-close-panel");
        if (closeBtn != null) closeBtn.clicked += ToggleResultsPanel;

        Button restartBtn = root.Q<Button>("btn-restart-lab");
        if (restartBtn != null) restartBtn.clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        Button exitBtn = root.Q<Button>("btn-exit-lab");
        if (exitBtn != null) exitBtn.clicked += () => SceneManager.LoadScene("Chemistry_Home"); 
    }

    private void SetupRowAutoCalculation(int rowIndex)
    {
        TextField vInput = root.Q<TextField>($"row{rowIndex}-v");
        TextField iInput = root.Q<TextField>($"row{rowIndex}-i");
        Label ratioOutput = root.Q<Label>($"row{rowIndex}-ratio");

        if (vInput == null || iInput == null || ratioOutput == null) return;

        EventCallback<ChangeEvent<string>> calculateRatio = (evt) =>
        {
            if (float.TryParse(vInput.value, out float v) && float.TryParse(iInput.value, out float i) && i != 0) 
            {
                ratioOutput.text = (v / i).ToString("F2"); 
            }
            else ratioOutput.text = "-";
        };

        vInput.RegisterValueChangedCallback(calculateRatio);
        iInput.RegisterValueChangedCallback(calculateRatio);
    }
    
    private void GradeAllRows()
    {
        int correctRowCount = 0;
        List<Vector2> studentDataPoints = new List<Vector2>();

        for (int i = 1; i <= 5; i++)
        {
            TextField vInput = root.Q<TextField>($"row{i}-v");
            TextField iInput = root.Q<TextField>($"row{i}-i");

            if (string.IsNullOrEmpty(vInput.value) || string.IsNullOrEmpty(iInput.value)) 
                continue; 

            if (float.TryParse(vInput.value, out float studentV) && float.TryParse(iInput.value, out float studentI))
            {
                float trueI_mA = (studentV / TargetResistor) * 1000f;
                float difference = Mathf.Abs(studentI - trueI_mA);
                float allowedVariance = trueI_mA * AllowedErrorMargin;

                if (difference <= allowedVariance)
                {
                    vInput.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 
                    iInput.style.backgroundColor = new StyleColor(new Color(0, 1, 0, 0.2f)); 
                    correctRowCount++;
                    studentDataPoints.Add(new Vector2(studentV, studentI)); // Capture data for graph
                }
                else
                {
                    vInput.style.backgroundColor = new StyleColor(new Color(1, 0, 0, 0.2f)); 
                    iInput.style.backgroundColor = new StyleColor(new Color(1, 0, 0, 0.2f));
                }
            }
        }

        if (correctRowCount == 5 && successPopup != null)
        {
            PlotDynamicGraph(studentDataPoints);
            successPopup.style.display = DisplayStyle.Flex;
            successPopup.style.scale = new StyleScale(new Vector2(0, 0));
            DOTween.To(() => Vector2.zero, x => successPopup.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBounce);
        }
    }

    private void PlotDynamicGraph(List<Vector2> dataPoints)
    {
        if (graphContainer == null) return;
        graphContainer.Clear(); 

        float maxV = 0.1f;
        float maxI = 0.1f;

        // Find the limits to scale the graph dynamically
        foreach (var point in dataPoints)
        {
            if (point.x > maxV) maxV = point.x;
            if (point.y > maxI) maxI = point.y;
        }

        // Add 10% padding so the highest point doesn't clip the edge
        maxV *= 1.1f;
        maxI *= 1.1f;

        // Generate the plot points
        foreach (var point in dataPoints)
        {
            VisualElement dataDot = new VisualElement();
            dataDot.style.position = Position.Absolute;
            dataDot.style.width = 12;
            dataDot.style.height = 12;
            dataDot.style.backgroundColor = new StyleColor(new Color(0, 1, 0.8f)); // Cyan dots
            dataDot.style.borderTopLeftRadius = 6;
            dataDot.style.borderTopRightRadius = 6;
            dataDot.style.borderBottomLeftRadius = 6;
            dataDot.style.borderBottomRightRadius = 6;

            // Calculate percentage position (0,0 is bottom-left in standard graphing)
            float leftPercent = (point.x / maxV) * 100f;
            float bottomPercent = (point.y / maxI) * 100f;

            dataDot.style.left = new Length(leftPercent, LengthUnit.Percent);
            dataDot.style.bottom = new Length(bottomPercent, LengthUnit.Percent);
            
            // Center the dot precisely on its coordinate
            dataDot.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), new Length(50, LengthUnit.Percent), 0));

            graphContainer.Add(dataDot);
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
                if (successPopup != null) successPopup.style.display = DisplayStyle.None;
            });
        }
    }
}