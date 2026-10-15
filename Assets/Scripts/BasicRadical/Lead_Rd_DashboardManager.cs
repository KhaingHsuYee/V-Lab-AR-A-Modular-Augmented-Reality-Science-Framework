using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 

public class Lead_Rd_DashboardManager : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement slidingContainer;
    
    // Reaction Dashboard UI Elements
    private Label reactionTitle;
    private Label reactionWhy;
    private Label reactionChemistry;
    private Label reactionEquation;

    private Vector2 touchStartPos;
    private int currentPage = 0; 

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Tell every element in this 3D dashboard to ignore UI clicks
        root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);

        slidingContainer = root.Q<VisualElement>("SlidingContainer");
        
        reactionTitle = root.Q<Label>("ReactionTitle");
        reactionWhy = root.Q<Label>("ReactionWhy");
        reactionChemistry = root.Q<Label>("ReactionChemistry");
        reactionEquation = root.Q<Label>("ReactionEquation");
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
        slidingContainer.style.translate = new StyleTranslate(new Translate(new Length(translationValue, LengthUnit.Percent), 0, 0));
    }

    public void ShowInstructions()
    {
        ChangePage(0);
    }

    public void UpdateReactionDashboard(string reagentName)
    {
        // Automatically swipe to the Reaction page when a chemical is selected
        ChangePage(1); 

        switch (reagentName)
        {
            case "Btn_HCl":
                reactionTitle.text = "Dilute Hydrochloric Acid (HCl)";
                reactionWhy.text = "Why it's used: Serves as the baseline diagnostic test for Group I cations.";
                reactionChemistry.text = "The Chemistry: Adding dilute HCl precipitates lead as insoluble Lead Chloride appearing as a characteristic white precipitate.";
                reactionEquation.text = "Pb(NO3)2 + 2HCl -> PbCl2 (white ppt) + 2HNO3";
                break;

            case "Btn_KI":
                reactionTitle.text = "Potassium Iodide (KI)";
                reactionWhy.text = "Why it's used: Takes advantage of Lead's unique temperature-dependent solubility curve.";
                reactionChemistry.text = "The Chemistry: KI yields a golden-yellow precipitate. When boiled, it dissolves completely. When cooled, it recrystallizes into shimmering golden flakes (Golden Rain).";
                reactionEquation.text = "Pb(NO3)2 + 2KI -> PbI2 (golden-yellow ppt) + 2KNO3";
                break;

            case "Btn_NaOH":
                reactionTitle.text = "Sodium Hydroxide (NaOH)";
                reactionWhy.text = "Why it's used: Verifies the amphoteric nature of Lead (reacts as both acid and base).";
                reactionChemistry.text = "The Chemistry: Forms white Lead Hydroxide. Upon adding excess NaOH, it acts as an acid and forms a clear, soluble complex salt called Sodium Plumbite.";
                reactionEquation.text = "1) Pb(NO3)2 + 2NaOH -> Pb(OH)2 (white ppt) + 2NaNO3\n2) Pb(OH)2 + 2NaOH (excess) -> Na2PbO2 (soluble) + 2H2O";
                break;

            case "Btn_K2CrO4":
                reactionTitle.text = "Potassium Chromate (K2CrO4)";
                reactionWhy.text = "Why it's used: Serves as the definitive, highly sensitive confirmatory test for Lead.";
                reactionChemistry.text = "The Chemistry: Chromate ions bind strongly to form an extremely stable, bright yellow precipitate completely insoluble in acetic acid.";
                reactionEquation.text = "Pb(NO3)2 + K2CrO4 -> PbCrO4 (bright yellow ppt) + 2KNO3";
                break;
        }
    }
}