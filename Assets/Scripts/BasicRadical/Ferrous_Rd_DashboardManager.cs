using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 

public class Ferrous_Rd_DashboardManager : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement slidingContainer;
    
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

        // Ignore UI clicks so they pass through to HUD
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

        // FIX 1: Support the New Input System for both mobile touches and PC testing
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
        
        // FIX 2: Use Percentages instead of raw pixels to prevent UI overlapping
        float translationValue = targetPage == 0 ? 0f : -50f; 
        slidingContainer.style.translate = new StyleTranslate(new Translate(new Length(translationValue, LengthUnit.Percent), 0, 0));
    }

    public void ShowInstructions()
    {
        ChangePage(0);
    }

    public void UpdateReactionDashboard(string reagentName)
    {
        ChangePage(1); 

        switch (reagentName)
        {
            case "Btn_HCl":
                reactionTitle.text = "Dilute Hydrochloric Acid (HCl)";
                reactionWhy.text = "Why it's used: Preliminary test for Group I cations.";
                reactionChemistry.text = "The Chemistry: Ferrous (Fe²⁺) belongs to Group III. It forms soluble chlorides, so no precipitate forms, confirming Group I is absent.";
                reactionEquation.text = "Fe²⁺ (aq) + HCl (aq) -> No Reaction";
                break;

            case "Btn_H2S":
                reactionTitle.text = "Hydrogen Sulphide (H₂S)";
                reactionWhy.text = "Why it's used: Tests for Group II cations in an acidic medium.";
                reactionChemistry.text = "The Chemistry: In acidic medium, H₂S concentration is too low to precipitate Group III metals like Iron. No precipitate forms.";
                reactionEquation.text = "FeSO₄ + H₂S (acidic) -> No Reaction";
                break;

            case "Btn_Buffer":
                reactionTitle.text = "Buffer (NH₄Cl + NH₄OH)";
                reactionWhy.text = "Why it's used: Precipitates Group III cations as hydroxides.";
                reactionChemistry.text = "The Chemistry: The basic buffer provides enough OH⁻ ions to precipitate Ferrous ions as Iron(II) hydroxide, which appears as a dirty green precipitate.";
                reactionEquation.text = "FeSO₄ + 2NH₄OH -> Fe(OH)₂ (Dirty Green ppt) + (NH₄)₂SO₄";
                break;

            case "Btn_K3FeCN6":
                reactionTitle.text = "Potassium Ferricyanide";
                reactionWhy.text = "Why it's used: A highly specific confirmatory test for Fe²⁺ ions.";
                reactionChemistry.text = "The Chemistry: Ferrous ions react with ferricyanide to form a distinct, deep blue precipitate commonly known as Turnbull's Blue.";
                reactionEquation.text = "3FeSO₄ + 2K₃[Fe(CN)₆] -> Fe₃[Fe(CN)₆]₂ (Deep Blue ppt) + 3K₂SO₄";
                break;

            case "Btn_NH42S":
                reactionTitle.text = "Ammonium Sulphide";
                reactionWhy.text = "Why it's used: Confirmatory test for Group III metals.";
                reactionChemistry.text = "The Chemistry: In a neutral/basic medium, there is enough sulphide concentration to precipitate iron as Iron(II) sulphide, a black solid.";
                reactionEquation.text = "FeSO₄ + (NH₄)₂S -> FeS (Black ppt) + (NH₄)₂SO₄";
                break;
        }
    }
}