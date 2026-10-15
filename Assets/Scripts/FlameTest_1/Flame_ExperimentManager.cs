using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; 
using DG.Tweening;

public class Flame_ExperimentManager : MonoBehaviour
{
    [Header("UI Documents")]
    [SerializeField] private UIDocument mainHudUIDocument; 
    [SerializeField] private UIDocument saltMenuUIDocument;
    [SerializeField] private UIDocument quizUIDocument; 

    [Header("Controllers")]
    [SerializeField] private Flame_ReactionController reactionController;

    [Header("3D Hologram Dashboard")]
    public ARDataController dashboardController; 
    public GameObject dashboardGameObject; 

    public enum ActionPhase { Pick, Burn }
    private ActionPhase currentPhase = ActionPhase.Pick;
    
    private Button btnAction; 

    private VisualElement hudContainer; 
    private Label statusTextLeft; 
    private Label statusTextRight; 
    private Button btnHome, btnReagent, btnReset;

    private VisualElement saltMenuPanel;
    private bool isMenuOpen = false; 

    // Popup UI
    private VisualElement exitPopupOverlay;
    private VisualElement exitPopupBox;
    private Label popupTitle, popupMessage;      
    private Button btnCancelExit, btnConfirmExit;

    private enum PopupState { None, Home, Reset }
    private PopupState currentPopupState = PopupState.None;

    // Quiz UI
    private VisualElement quizOverlay;
    private TextField inputElement;
    private Label labelFeedback;
    private Button btnSubmit;
    private string correctElementName = "";

    // --- Experiment Progress Trackers ---
    private bool testedNaCl = false;
    private bool testedLiCl = false;
    private bool testedCuCl2 = false;
    private bool testedKCl = false;
    private bool guessedUnknown = false;

    public void InitializeLab()
    {
        if (mainHudUIDocument != null)
        {
            mainHudUIDocument.gameObject.SetActive(true);
            var root = mainHudUIDocument.rootVisualElement;
            if (root != null)
            {
                hudContainer = root.Q<VisualElement>("hud-container"); 
                statusTextLeft = root.Q<Label>("status-text-left");
                statusTextRight = root.Q<Label>("status-text-right");

                btnHome = root.Q<Button>("btn-home");
                btnReagent = root.Q<Button>("btn-reagent");
                btnReset = root.Q<Button>("btn-reset");

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                exitPopupBox = root.Q<VisualElement>("exit-popup-box");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;
                if (btnReagent != null) btnReagent.clicked += ToggleSaltMenu;
                
                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;

                btnAction = root.Q<Button>("btn-add");
                if (btnAction != null)
                {
                    btnAction.style.display = DisplayStyle.None;
                    btnAction.clicked += OnActionButtonClicked;
                }
            }
        }

        if (saltMenuUIDocument != null)
        {
            saltMenuUIDocument.gameObject.SetActive(true);
            var menuRoot = saltMenuUIDocument.rootVisualElement;
            if (menuRoot != null)
            {
                saltMenuPanel = menuRoot.Q<VisualElement>("Salt_Menu_Panel");
                if (saltMenuPanel != null) saltMenuPanel.style.display = DisplayStyle.None;

                BindSaltButton(menuRoot, "Btn_LiCl", "Lithium Chloride (LiCl)");
                BindSaltButton(menuRoot, "Btn_NaCl", "Sodium Chloride (NaCl)");
                BindSaltButton(menuRoot, "Btn_CuCl2", "Copper(II) Chloride (CuCl2)");
                BindSaltButton(menuRoot, "Btn_KCl", "Potassium Chloride (KCl)");
                BindSaltButton(menuRoot, "Btn_Unknown", "Unknown Salt");
            }
        }

        if (quizUIDocument != null)
        {
            quizUIDocument.gameObject.SetActive(true);
            var quizRoot = quizUIDocument.rootVisualElement;
            if (quizRoot != null)
            {
                quizOverlay = quizRoot.Q<VisualElement>("Quiz_Overlay");
                inputElement = quizRoot.Q<TextField>("Input_Element");
                labelFeedback = quizRoot.Q<Label>("Label_Feedback");
                btnSubmit = quizRoot.Q<Button>("Btn_Submit");

                if (btnSubmit != null) btnSubmit.clicked += CheckQuizAnswer;

                if (inputElement != null)
                {
                    inputElement.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
                }
            }
        }

        if (hudContainer != null) hudContainer.style.display = DisplayStyle.Flex;
        UpdateStatusText("Flame Test Active", "Select a salt from the menu to begin.");

        // ==========================================
        // FORCE THE DASHBOARD TO SPAWN ON START
        // ==========================================
        if (dashboardGameObject != null && dashboardController != null)
        {
            GameObject activeBurner = GameObject.FindWithTag("Burner");
            if (activeBurner != null)
            {
                // Position above the burner at exactly 0.25f height
                Vector3 spawnOffset = (Vector3.up * 0.25f) + (activeBurner.transform.forward * 0.05f);
                dashboardGameObject.transform.position = activeBurner.transform.position + spawnOffset;

                // Face the camera directly
                if (Camera.main != null)
                {
                    Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                    dashboardGameObject.transform.LookAt(targetPosition);
                    dashboardGameObject.transform.Rotate(0, 180, 0); 
                }

                dashboardGameObject.SetActive(true);
                dashboardController.ShowInstructions();
            }
        }
    }

    private void BindSaltButton(VisualElement root, string btnName, string displayName)
    {
        Button b = root.Q<Button>(btnName);
        if (b != null)
        {
            b.clicked += () => {
                if (reactionController != null)
                {
                    if (btnName == "Btn_Unknown")
                    {
                        string[] randomSalts = { "Btn_LiCl", "Btn_NaCl", "Btn_CuCl2", "Btn_KCl" };
                        reactionController.SelectSalt(randomSalts[Random.Range(0, randomSalts.Length)], true);
                    }
                    else
                    {
                        reactionController.SelectSalt(btnName, false);
                    }
                    
                    ToggleSaltMenu();
                    SetButtonStatePick();
                    UpdateStatusText("Salt Selected", $"Target: {displayName}");
                }
            };
        }
    }

    private void OnActionButtonClicked()
    {
        if (reactionController == null || reactionController.isAnimating) return;

        if (currentPhase == ActionPhase.Pick)
        {
            reactionController.ExecutePickSample();
        }
        else if (currentPhase == ActionPhase.Burn)
        {
            reactionController.ExecuteBurnSample();
            HideActionButton(); 
        }
    }

    public void MarkSaltTested(string saltId)
    {
        switch (saltId)
        {
            case "Btn_NaCl": testedNaCl = true; break;
            case "Btn_LiCl": testedLiCl = true; break;
            case "Btn_CuCl2": testedCuCl2 = true; break;
            case "Btn_KCl": testedKCl = true; break;
        }
    }

    public void OpenUnknownSaltQuiz(string actualSaltId)
    {
        switch (actualSaltId)
        {
            case "Btn_NaCl": correctElementName = "sodium"; break;
            case "Btn_LiCl": correctElementName = "lithium"; break;
            case "Btn_CuCl2": correctElementName = "copper"; break;
            case "Btn_KCl": correctElementName = "potassium"; break;
        }

        if (quizOverlay != null)
        {
            inputElement.value = "";
            labelFeedback.text = "";
            quizOverlay.style.display = DisplayStyle.Flex;
            quizOverlay.style.opacity = 0f;
            DOTween.To(() => 0f, x => quizOverlay.style.opacity = x, 1f, 0.4f).OnComplete(() => {
                inputElement.Focus(); 
            });
        }
    }

    private void OnInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            CheckQuizAnswer();
        }
    }

    private void CheckQuizAnswer()
    {
        string userAnswer = inputElement.value.Trim().ToLower();
        if (string.IsNullOrEmpty(userAnswer)) return;

        if (userAnswer == correctElementName)
        {
            labelFeedback.text = $"Correct! The element is {correctElementName.ToUpper()}.";
            labelFeedback.style.color = new StyleColor(new Color(0.2f, 0.9f, 0.2f)); 
            guessedUnknown = true; 
            DOVirtual.DelayedCall(2.5f, CloseQuiz);
        }
        else
        {
            labelFeedback.text = "Incorrect. Returning to lab to try again...";
            labelFeedback.style.color = new StyleColor(new Color(0.9f, 0.3f, 0.3f)); 
            DOVirtual.DelayedCall(2.5f, CloseQuiz); 
        }
    }

    private void CloseQuiz()
    {
        if (quizOverlay != null)
        {
            DOTween.To(() => 1f, x => quizOverlay.style.opacity = x, 0f, 0.3f).OnComplete(() => {
                quizOverlay.style.display = DisplayStyle.None;
                SetButtonStatePick(); 
                UpdateStatusText("Test Complete", "Ready for next sample.");

                if (testedNaCl && testedLiCl && testedCuCl2 && testedKCl && guessedUnknown)
                {
                    ShowSuccessPopup();
                }
            });
        }
    }

    public void SetButtonStatePick()
    {
        if (btnAction != null) { btnAction.text = "PICK"; currentPhase = ActionPhase.Pick; btnAction.style.display = DisplayStyle.Flex; }
    }

    public void SetButtonStateBurn()
    {
        if (btnAction != null) { btnAction.text = "BURN"; currentPhase = ActionPhase.Burn; btnAction.style.display = DisplayStyle.Flex; }
    }

    public void HideActionButton()
    {
        if (btnAction != null) btnAction.style.display = DisplayStyle.None;
    }

    public void UpdateStatusText(string leftMessage, string rightMessage)
    {
        if (statusTextLeft != null) statusTextLeft.text = leftMessage;
        if (statusTextRight != null) statusTextRight.text = rightMessage;
    }

    private void ToggleSaltMenu()
    {
        if (saltMenuPanel == null) return;
        if (!isMenuOpen)
        {
            isMenuOpen = true;
            saltMenuPanel.style.display = DisplayStyle.Flex;
            DOTween.To(() => 0f, x => saltMenuPanel.style.opacity = x, 1f, 0.3f);
            DOTween.To(() => -50f, x => saltMenuPanel.style.translate = new StyleTranslate(new Translate(new Length(x, LengthUnit.Pixel), 0, 0)), 0f, 0.3f);
        }
        else
        {
            isMenuOpen = false;
            DOTween.To(() => 1f, x => saltMenuPanel.style.opacity = x, 0f, 0.2f);
            DOTween.To(() => 0f, x => saltMenuPanel.style.translate = new StyleTranslate(new Translate(new Length(x, LengthUnit.Pixel), 0, 0)), -50f, 0.2f).OnComplete(() => saltMenuPanel.style.display = DisplayStyle.None);
        }
    }

    private void PromptGoHome() 
    { 
        currentPopupState = PopupState.Home; 
        if (popupTitle != null) popupTitle.text = "Exit Experiment?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to stop the experiment and return to the main menu?";
        if (btnCancelExit != null) btnCancelExit.text = "Cancel"; 
        if (btnConfirmExit != null) btnConfirmExit.text = "Exit"; 
        ShowPopup(); 
    }

    private void PromptReset() 
    { 
        currentPopupState = PopupState.Reset; 
        if (popupTitle != null) popupTitle.text = "Restart Experiment?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to clear the setup and start over from the beginning?";
        if (btnCancelExit != null) btnCancelExit.text = "Cancel"; 
        if (btnConfirmExit != null) btnConfirmExit.text = "Restart"; 
        ShowPopup(); 
    }

    private void ShowSuccessPopup()
    {
        currentPopupState = PopupState.Home; 
        if (popupTitle != null) popupTitle.text = "Experiment Complete!";
        if (popupMessage != null) popupMessage.text = "Congratulations! You have tested all known salts and correctly identified the unknown element.";
        if (btnCancelExit != null) btnCancelExit.text = "Stay in Lab"; 
        if (btnConfirmExit != null) btnConfirmExit.text = "Exit";
        ShowPopup();
    }

    private void ShowPopup() 
    { 
        if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.Flex; 
        if (btnAction != null) btnAction.style.display = DisplayStyle.None;
    }

    private void ClosePopup() 
    { 
        if (exitPopupOverlay != null) exitPopupOverlay.style.display = DisplayStyle.None; 
        if (currentPhase == ActionPhase.Pick) SetButtonStatePick();
        else if (currentPhase == ActionPhase.Burn) SetButtonStateBurn();
    }

    private void ConfirmPopupAction()
    {
        if (currentPopupState == PopupState.Home) SceneManager.LoadScene("Chemistry_Home"); 
        else if (currentPopupState == PopupState.Reset) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}