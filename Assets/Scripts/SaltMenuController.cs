using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SaltMenuController : MonoBehaviour
{
    public Flame_LabChemicalManager chemicalManager; 
    private VisualElement root;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            root = uiDocument.rootVisualElement;

            // Setup buttons
            SetupButton("Btn_LiCl", "LiCl");
            SetupButton("Btn_NaCl", "NaCl");
            SetupButton("Btn_CuCl2", "CuCl2");
            SetupButton("Btn_KCl", "KCl");
            SetupButton("Btn_Unknown", "Unknown");
        }
    }

    void SetupButton(string btnName, string saltName)
    {
        Button btn = root.Q<Button>(btnName);
        if (btn != null)
        {
            btn.clicked += () => 
            {
                // 1. Update visual selection
                UpdateSelectionVisuals(btn);
                
                // 2. Trigger your manager logic
                if (saltName == "Unknown")
                    chemicalManager.SelectUnknownSalt();
                else
                    chemicalManager.SelectSalt(saltName);
            };
        }
    }

    void UpdateSelectionVisuals(Button selectedBtn)
    {
        // Find all buttons with the salt row class
        var allButtons = root.Query<Button>(className: "salt-row-button").ToList();
        
        // Remove 'active-selection' from everyone
        foreach (var btn in allButtons)
        {
            btn.RemoveFromClassList("active-selection");
        }

        // Add 'active-selection' to the clicked one
        selectedBtn.AddToClassList("active-selection");
    }
}