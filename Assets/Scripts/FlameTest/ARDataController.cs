using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 

public class ARDataController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement slidingContainer;
    private Vector2 touchStartPos;
    private int currentPage = 0; 

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Tell every element in this 3D dashboard to ignore UI clicks
        root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);

        slidingContainer = root.Q<VisualElement>("SlidingContainer");
    }

    private void Update()
    {
        Vector2 currentPos = Vector2.zero;
        bool wasPressedThisFrame = false;
        bool wasReleasedThisFrame = false;

        // Support the New Input System for both mobile touches and PC testing
        if (Touchscreen.current != null)
        {
            currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            wasPressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            wasReleasedThisFrame = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        }
        else if (Mouse.current != null)
        {
            currentPos = Mouse.current.position.ReadValue();
            wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
        }

        if (wasPressedThisFrame)
        {
            touchStartPos = currentPos; 
        }
        else if (wasReleasedThisFrame)
        {
            float swipeDistance = currentPos.x - touchStartPos.x;
            
            if (swipeDistance > 100f && currentPage == 1) ChangePage(0);
            else if (swipeDistance < -100f && currentPage == 0) ChangePage(1);
        }
    }

    private void ChangePage(int targetPage)
    {
        currentPage = targetPage;
        
        // Use Percentages instead of raw pixels to prevent UI overlapping
        float translationValue = targetPage == 0 ? 0f : -50f; 
        if (slidingContainer != null)
        {
            slidingContainer.style.translate = new StyleTranslate(new Translate(new Length(translationValue, LengthUnit.Percent), 0, 0));
        }
    }

    public void ShowInstructions()
    {
        ChangePage(0);
    }

    public void Populate(ElementData data)
    {
        // Automatically swipe to the Reaction data page
        ChangePage(1); 

        var root = uiDocument.rootVisualElement;

        // 1. Inject the ScriptableObject data into the UI labels
        root.Q<Label>("Title").text = $"Sample: {data.sampleName} ({data.formula})";
        root.Q<Label>("AtomicData").text = $"Protons: {data.protons} | Neutrons: {data.neutrons}";
        root.Q<Label>("Metrics").text = $"λ: {data.wavelength} | E: {data.photonEnergy} | Trans: {data.transition}";

        // 2. Spectral Graph Drawing
        var graphContainer = root.Q<VisualElement>("GraphContainer");
        
        // Overwrite the visual content callback to redraw the graph for the new element
        graphContainer.generateVisualContent = (ctx) => {
            var p = ctx.painter2D;
            p.strokeColor = new Color(0.26f, 1f, 1f); // Cyan Laser Color
            p.lineWidth = 1.5f;
            
            float width = graphContainer.contentRect.width;
            float height = graphContainer.contentRect.height;
            if (data.spectralData == null || data.spectralData.Length == 0) return;
            float step = width / (data.spectralData.Length - 1);

            p.BeginPath();
            for (int i = 0; i < data.spectralData.Length; i++) {
                float x = i * step;
                float y = height - (data.spectralData[i] * height);
                if (i == 0) p.MoveTo(new Vector2(x, y));
                else p.LineTo(new Vector2(x, y));
            }
            p.Stroke();
        };

        // Force the UI to redraw the graph immediately
        graphContainer.MarkDirtyRepaint();
    }
}