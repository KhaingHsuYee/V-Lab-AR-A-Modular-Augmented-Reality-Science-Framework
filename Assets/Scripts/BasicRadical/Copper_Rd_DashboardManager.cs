using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 

public class Copper_Rd_DashboardManager : MonoBehaviour
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
        // Automatically swipe to the Reaction page when a chemical is selected
        ChangePage(1); 

        switch (reagentName)
        {
            case "Btn_HCl":
                reactionTitle.text = "Dilute Hydrochloric Acid (HCl)";
                reactionWhy.text = "Why it's used: Preliminary test for Group I cations.";
                reactionChemistry.text = "The Chemistry: Copper(II) belongs to Group II, not Group I. Therefore, no precipitate forms when dilute HCl is added, confirming the absence of Lead(II).";
                reactionEquation.text = "Cu²⁺ (aq) + HCl (aq) -> No Reaction";
                break;

            case "Btn_H2S":
                reactionTitle.text = "Hydrogen Sulphide (H₂S)";
                reactionWhy.text = "Why it's used: Precipitates Group II cations in an acidic medium.";
                reactionChemistry.text = "The Chemistry: Copper reacts with hydrogen sulphide gas to form Copper(II) sulphide, which is highly insoluble and appears as a dense black precipitate.";
                reactionEquation.text = "CuSO₄ + H₂S -> CuS (Black ppt) + H₂SO₄";
                break;

            case "Btn_NaOH":
                reactionTitle.text = "Sodium Hydroxide (NaOH)";
                reactionWhy.text = "Why it's used: Confirmatory test reacting with bases and heat.";
                reactionChemistry.text = "The Chemistry: Forms a pale blue precipitate of Copper(II) hydroxide. Upon heating, this unstable hydroxide decomposes into water and black Copper(II) oxide.";
                reactionEquation.text = "1) CuSO₄ + 2NaOH -> Cu(OH)₂ (Pale Blue ppt) + Na₂SO₄\n2) Cu(OH)₂ + Heat -> CuO (Black ppt) + H₂O";
                break;

            case "Btn_Fe":
                reactionTitle.text = "Iron Nail (Fe)";
                reactionWhy.text = "Why it's used: Demonstrates a classic single displacement reaction.";
                reactionChemistry.text = "The Chemistry: Iron is higher in the reactivity series than copper. The iron metal dissolves into solution while solid copper is deposited onto the nail's surface.";
                reactionEquation.text = "Fe (s) + CuSO₄ (aq) -> FeSO₄ (aq) + Cu (s)";
                break;

            case "Btn_K4FeCN6":
                reactionTitle.text = "Potassium Ferrocyanide";
                reactionWhy.text = "Why it's used: A highly sensitive confirmatory test for Copper ions.";
                reactionChemistry.text = "The Chemistry: Copper ions react with ferrocyanide ions to form a distinct, complex chocolate-brown precipitate of Copper(II) ferrocyanide.";
                reactionEquation.text = "2CuSO₄ + K₄[Fe(CN)₆] -> Cu₂[Fe(CN)₆] (Brown ppt) + 2K₂SO₄";
                break;
        }
    }
}