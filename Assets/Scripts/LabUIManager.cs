using UnityEngine;

public class LabUIManager : MonoBehaviour
{
    [Header("Main Canvas Layer")]
    // Assign your parent Canvas or Master Panel GameObject here in the Inspector
    [SerializeField] private GameObject bottomMenuPanel; 

    [Header("Tab Content Panels (Button Groups)")]
    [SerializeField] private GameObject flaskContentPanel;
    [SerializeField] private GameObject indicatorContentPanel;
    [SerializeField] private GameObject acidBaseContentPanel;

    private void Awake()
    {
        // Force the Lab Menu canvas to be inactive completely before starting/visible interactions
        if (bottomMenuPanel != null)
        {
            bottomMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("LabUIManager: Bottom Menu Panel is not assigned in the inspector!");
        }
    }

    /// <summary>
    /// Toggles the menu visibility. Triggered by your FlaskInteraction script on double click/touch.
    /// </summary>
    /// <param name="flaskPosition">Passed vector position of the interactive flask object.</param>
    public void ToggleIndicatorMenu(Vector3 flaskPosition)
    {
        if (bottomMenuPanel != null)
        {
            // Check if the panel is currently active
            bool isMenuCurrentlyOpen = bottomMenuPanel.activeSelf;

            if (isMenuCurrentlyOpen)
            {
                // If it's already open, close it
                bottomMenuPanel.SetActive(false);
                Debug.Log($"Lab Menu Deactivated via Flask interaction at coordinates: {flaskPosition}");
            }
            else
            {
                // If it's closed, open it
                bottomMenuPanel.SetActive(true);
                
                // Automatically display the first sub-menu layout configuration context (e.g., Tab 1)
                SwitchTab(1); 
                
                Debug.Log($"Lab Menu Activated via Flask interaction at coordinates: {flaskPosition}");
            }
        }
    }

    // --- TAB TOGGLE CONTROLLER ---
    public void SwitchTab(int tabIndex)
    {
        // Deactivate all content windows first
        flaskContentPanel.SetActive(false);
        indicatorContentPanel.SetActive(false);
        acidBaseContentPanel.SetActive(false);

        // Turn on the selected content window group
        if (tabIndex == 1) flaskContentPanel.SetActive(true);
        else if (tabIndex == 2) indicatorContentPanel.SetActive(true);
        else if (tabIndex == 3) acidBaseContentPanel.SetActive(true);
    }

    // --- BUTTON CALLBACKS FOR EXPERIMENT LAB OPTIONS ---
    public void OnClickFlask50ml() { Debug.Log("Selected: 50ml Conical Flask"); }
    public void OnClickFlask100ml() { Debug.Log("Selected: 100ml Conical Flask"); }
    public void OnClickFlask250ml() { Debug.Log("Selected: 250ml Conical Flask"); }

    public void OnClickMethylOrange() { Debug.Log("Selected Indicator: Methyl Orange"); }
    public void OnClickPhenolphthalein() { Debug.Log("Selected Indicator: Phenolphthalein"); }

    public void OnClickHCl() { Debug.Log("Selected Reagent: Hydrochloric Acid (HCl)"); }
    public void OnClickNaOH() { Debug.Log("Selected Reagent: Sodium Hydroxide (NaOH)"); }
}