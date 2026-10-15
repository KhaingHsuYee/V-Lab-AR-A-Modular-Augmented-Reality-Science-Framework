using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; 
using DG.Tweening;

public class Lead_Rd_ExperimentManager : MonoBehaviour
{
    [Header("UI Documents")]
    [SerializeField] private UIDocument mainHudUIDocument; 
    [SerializeField] private UIDocument reagentMenuUIDocument;

    [Header("3D Hologram Dashboard")]
    public Lead_Rd_DashboardManager dashboardManager; 
    public GameObject dashboardGameObject; 

    [Header("New Add Button Logic")]
    [SerializeField] private Lead_Rd_ChemicalReactionController reactionController;
    [SerializeField] private GameObject beakerPrefab;
    private Button btnAdd;
    private Tween pulseTween;
    private GameObject currentBeakerInstance;

    public AudioClip beakerDropSound;
    public AudioSource sfxAudioSource; 

    // Action State tracker
    public enum ActionButtonMode { Add, Heat }
    private ActionButtonMode currentButtonMode = ActionButtonMode.Add;

    // HUD Elements
    private VisualElement hudContainer; 
    private Label statusTextLeft; 
    private Label statusTextRight; 
    
    private Button btnHome;
    private Button btnReagent;
    private Button btnReset;
    private Button btnHelp;

    private VisualElement exitPopupOverlay;
    private VisualElement exitPopupBox;
    private Label popupTitle;        
    private Label popupMessage;      
    private Button btnCancelExit;
    private Button btnConfirmExit;

    private VisualElement reagentMenuPanel;
    private bool isReagentMenuOpen = false; 
    private bool isLabActive = false; 

    private enum PopupState { None, Home, Reset }
    private PopupState currentPopupState = PopupState.None;

    private VisualElement outOfTubesOverlay;
    private VisualElement outOfTubesBox;
    private Button btnRestartOutOfTubes;
    private Button btnExitOutOfTubes;

    private VisualElement successOverlay;
    private VisualElement successBox;
    private Button btnStaySuccess;
    private Button btnExitSuccess;

    // --- Specific Experiment Progress Trackers ---
    public bool testedHCl = false;
    public bool testedK2CrO4 = false;
    public bool excessNaOH = false;
    public bool heatedKI = false;
    private bool hasShownCustomSuccess = false;

    private void Awake()
    {
        UnityEngine.XR.ARFoundation.LoaderUtility.Initialize();
    }

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
                btnHelp = root.Q<Button>("btn-help");

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                exitPopupBox = root.Q<VisualElement>("exit-popup-box");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;
                if (btnReagent != null) btnReagent.clicked += ToggleReagentMenu;
                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;

                outOfTubesOverlay = root.Q<VisualElement>("out-of-tubes-overlay");
                outOfTubesBox = root.Q<VisualElement>("out-of-tubes-box");
                btnRestartOutOfTubes = root.Q<Button>("btn-restart-out");
                btnExitOutOfTubes = root.Q<Button>("btn-exit-out");

                if (btnRestartOutOfTubes != null) btnRestartOutOfTubes.clicked += RestartARExperiment;
                if (btnExitOutOfTubes != null) btnExitOutOfTubes.clicked += () => { SceneManager.LoadScene("Chemistry_Home"); };

                successOverlay = root.Q<VisualElement>("success-overlay");
                successBox = root.Q<VisualElement>("success-box");
                btnStaySuccess = root.Q<Button>("btn-stay-success");
                btnExitSuccess = root.Q<Button>("btn-exit-success");

                if (btnStaySuccess != null) btnStaySuccess.clicked += HideSuccessPopup;
                if (btnExitSuccess != null) btnExitSuccess.clicked += () => { SceneManager.LoadScene("Chemistry_Home"); };

                btnAdd = root.Q<Button>("btn-add");
                if (btnAdd != null)
                {
                    btnAdd.style.display = DisplayStyle.None;
                    btnAdd.clicked += () => {
                        
                        if (currentButtonMode == ActionButtonMode.Heat)
                        {
                            if (reactionController != null)
                            {
                                reactionController.ApplyHeatToTube(reactionController.GetActiveTube());
                                HideAddButton();
                            }
                            return;
                        }

                        if (reactionController != null && reactionController.isDropping) return;

                        if (reactionController != null && reactionController.dropper != null)
                        {
                            if (currentBeakerInstance != null)
                            {
                                Vector3 dropperStartPos = currentBeakerInstance.transform.position + (Vector3.up * 0.5f);
                                reactionController.dropper.transform.position = dropperStartPos;
                            }
                            reactionController.dropper.SetActive(true);
                        }
                        
                        if (currentBeakerInstance != null)
                            reactionController.StartReactionSequence(currentBeakerInstance);
                    };
                }
            }
        }

        if (reagentMenuUIDocument != null)
        {
            reagentMenuUIDocument.gameObject.SetActive(true);
            var reagentRoot = reagentMenuUIDocument.rootVisualElement;

            string[] leadReagents = { "Btn_HCl", "Btn_KI", "Btn_NaOH", "Btn_K2CrO4" };
            FilterReagentMenu(reagentRoot, leadReagents);
            
            if (reagentRoot != null)
            {
                reagentMenuPanel = reagentRoot.Q<VisualElement>("Reagent_Menu_Panel");
                
                if (reagentMenuPanel != null)
                {
                    reagentMenuPanel.style.display = DisplayStyle.None;
                    reagentMenuPanel.style.opacity = 0f;
                    reagentMenuPanel.style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Pixel), 0, 0));
                }

                string[] btnNames = { "Btn_HCl", "Btn_KI", "Btn_NaOH", "Btn_K2CrO4" };
                foreach (string name in btnNames)
                {
                    Button b = reagentRoot.Q<Button>(name);
                    if (b != null) b.clicked += () => {
                        
                        if (reactionController != null)
                        {
                            reactionController.SwapTubeIfNeeded(name, (activeTube) => 
                            {
                                if (activeTube != null)
                                {
                                    if (currentBeakerInstance != null) Destroy(currentBeakerInstance);

                                    Vector3 beakerOffset = Camera.main.transform.right * 0.15f; 
                                    Vector3 finalPosition = activeTube.position + beakerOffset;
                                    finalPosition.y = activeTube.position.y; 
                                    
                                    Vector3 spawnPosition = finalPosition + (Vector3.up * 0.5f); 

                                    currentBeakerInstance = Instantiate(beakerPrefab, spawnPosition, Quaternion.identity);
                                    currentBeakerInstance.transform.localScale = beakerPrefab.transform.localScale;
                                    
                                    reactionController.ApplyReagentColorToBeaker(currentBeakerInstance, name);

                                    currentBeakerInstance.transform.DOMoveY(finalPosition.y, 0.5f)
                                        .SetEase(Ease.OutBounce)
                                        .OnComplete(() => {
                                            if (sfxAudioSource != null && beakerDropSound != null) {
                                                sfxAudioSource.PlayOneShot(beakerDropSound);
                                            }
                                        });
                                    
                                    reactionController.SetReagentType(name);
                                    
                                    if (dashboardGameObject != null)
                                    {
                                        // Anchors to active tube with lower height of 0.25f
                                        // To this:
                                        Vector3 spawnOffset = (Vector3.up * 0.25f) + (activeTube.forward * 0.05f);                                        dashboardGameObject.transform.position = activeTube.position + spawnOffset;
                                        
                                        // Restores dynamic rotation so it faces the camera
                                        if (Camera.main != null)
                                        {
                                            Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                                            dashboardGameObject.transform.LookAt(targetPosition);
                                            dashboardGameObject.transform.Rotate(0, 180, 0); 
                                        }

                                        dashboardGameObject.SetActive(true);
                                        
                                        if(dashboardManager != null) dashboardManager.UpdateReactionDashboard(name);
                                    }

                                    OnReagentSelected(currentBeakerInstance);
                                }
                            });
                        }
                    };
                }
            }
        }

        if (hudContainer != null)
        {
            hudContainer.style.display = DisplayStyle.Flex;
            DOTween.To(() => 0f, x => hudContainer.style.opacity = x, 1f, 1f); 
        }

        UpdateStatusText("Experiment starts!", "Current Sample: Unknown (Pb++)");
        isLabActive = true;

        // Force the dashboard to spawn and show Instructions immediately!
        if (dashboardGameObject != null && reactionController != null && reactionController.tubeParents.Length > 0)
        {
            Transform firstTube = reactionController.tubeParents[0];
            if (firstTube != null)
            {
                // To this:
                Vector3 spawnOffset = (Vector3.up * 0.25f) + (firstTube.forward * 0.05f);
                dashboardGameObject.transform.position = firstTube.position + spawnOffset;

                if (Camera.main != null)
                {
                    Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                    dashboardGameObject.transform.LookAt(targetPosition);
                    dashboardGameObject.transform.Rotate(0, 180, 0); 
                }

                dashboardGameObject.SetActive(true);
                
                if (dashboardManager != null) dashboardManager.ShowInstructions();
            }
        }
    }

    public void OnReagentSelected(GameObject beaker)
    {
        currentBeakerInstance = beaker;
        ResetAddButton(); 
        btnAdd.style.display = DisplayStyle.Flex;
        
        if (pulseTween != null) pulseTween.Kill();
        pulseTween = DOTween.To(() => new Vector2(1, 1), 
            x => btnAdd.style.scale = new StyleScale(new Vector2(x.x, x.y)), 
            new Vector2(1.1f, 1.1f), 0.8f)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        ToggleReagentMenu();
    }

    public void ShowHeatButton()
    {
        if (btnAdd != null)
        {
            btnAdd.text = "HEAT";
            btnAdd.style.backgroundColor = new StyleColor(new Color(0.9f, 0.4f, 0.0f)); 
            currentButtonMode = ActionButtonMode.Heat;
            btnAdd.style.display = DisplayStyle.Flex;
            
            if (pulseTween != null) pulseTween.Kill();
            pulseTween = DOTween.To(() => new Vector2(1, 1), 
                x => btnAdd.style.scale = new StyleScale(new Vector2(x.x, x.y)), 
                new Vector2(1.1f, 1.1f), 0.8f)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void ResetAddButton()
    {
        if (btnAdd != null)
        {
            btnAdd.text = "ADD";
            btnAdd.style.backgroundColor = new StyleColor(StyleKeyword.Null); 
            currentButtonMode = ActionButtonMode.Add;
        }
    }

    public void HideAddButton()
    {
        if (btnAdd != null) btnAdd.style.display = DisplayStyle.None;
        if (pulseTween != null) pulseTween.Kill();
    }

    public void UpdateStatusText(string leftMessage, string rightMessage)
    {
        if (statusTextLeft != null) statusTextLeft.text = leftMessage;
        if (statusTextRight != null) statusTextRight.text = rightMessage;
    }

    private void PromptGoHome()
    {
        if (!isLabActive) return;
        currentPopupState = PopupState.Home;
        if (popupTitle != null) popupTitle.text = "Exit Experiment?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to stop the experiment and return to the main menu?";
        ShowPopup();
    }

    private void PromptReset()
    {
        if (!isLabActive) return;
        currentPopupState = PopupState.Reset;
        if (popupTitle != null) popupTitle.text = "Restart Experiment?";
        if (popupMessage != null) popupMessage.text = "Are you sure you want to clear the test tubes and start over from the beginning?";
        ShowPopup();
    }

    private void ShowPopup()
    {
        if (exitPopupOverlay == null || exitPopupBox == null) return;
        exitPopupOverlay.style.display = DisplayStyle.Flex;
        exitPopupBox.style.scale = new StyleScale(new Vector2(0, 0));
        DOTween.To(() => Vector2.zero, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack);
    }

    private void ClosePopup()
    {
        if (exitPopupOverlay == null || exitPopupBox == null) return;
        currentPopupState = PopupState.None; 
        DOTween.To(() => Vector2.one, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
            exitPopupOverlay.style.display = DisplayStyle.None;
        });
    }

    private void ConfirmPopupAction()
    {
        if (currentPopupState == PopupState.Home) SceneManager.LoadScene("Chemistry_Home"); 
        else if (currentPopupState == PopupState.Reset) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ToggleReagentMenu()
    {
        if (!isLabActive || reagentMenuPanel == null) return;

        if (!isReagentMenuOpen)
        {
            isReagentMenuOpen = true;
            reagentMenuPanel.style.display = DisplayStyle.Flex;
            DOTween.To(() => 0f, x => reagentMenuPanel.style.opacity = x, 1f, 0.35f).SetEase(Ease.OutQuad);
            DOTween.To(() => -50f, x => reagentMenuPanel.style.translate = new StyleTranslate(new Translate(new Length(x, LengthUnit.Pixel), 0, 0)), 0f, 0.4f).SetEase(Ease.OutBack);
            UpdateStatusText("Reagent Menu Opened.", "Select a reagent to add to the sample.");
        }
        else
        {
            isReagentMenuOpen = false;
            DOTween.To(() => 1f, x => reagentMenuPanel.style.opacity = x, 0f, 0.25f).SetEase(Ease.InQuad);
            DOTween.To(() => 0f, x => reagentMenuPanel.style.translate = new StyleTranslate(new Translate(new Length(x, LengthUnit.Pixel), 0, 0)), -50f, 0.25f).SetEase(Ease.InQuad).OnComplete(() => {
                reagentMenuPanel.style.display = DisplayStyle.None;
            });
            UpdateStatusText("Reagent Menu Closed.", "Status: Waiting for input");
        }
    }

    public void ShowOutOfTubesPopup()
    {
        if (outOfTubesOverlay != null && outOfTubesBox != null)
        {
            outOfTubesOverlay.style.display = DisplayStyle.Flex;
            outOfTubesBox.style.scale = new StyleScale(new Vector2(0, 0));
            DOTween.To(() => Vector2.zero, x => outOfTubesBox.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack);
        }
    }

    public void ShowSuccessPopup()
    {
        if (successOverlay != null && successBox != null)
        {
            successOverlay.style.display = DisplayStyle.Flex;
            successBox.style.scale = new StyleScale(new Vector2(0, 0));
            DOTween.To(() => Vector2.zero, x => successBox.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void HideSuccessPopup()
    {
        if (successOverlay != null && successBox != null)
        {
            DOTween.To(() => Vector2.one, x => successBox.style.scale = new StyleScale(x), Vector2.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                successOverlay.style.display = DisplayStyle.None;
            });
        }
    }

    public void RestartARExperiment()
    {
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void FilterReagentMenu(VisualElement reagentRoot, string[] requiredReagents)
    {
        string[] allButtons = { 
            "Btn_HCl", "Btn_KI", "Btn_NaOH", "Btn_K2CrO4", 
            "Btn_H2S", "Btn_Fe", "Btn_K4FeCN6", "Btn_K3FeCN6", 
            "Btn_Buffer", "Btn_NH42S" 
        };

        foreach (string btnName in allButtons)
        {
            Button b = reagentRoot.Q<Button>(btnName);
            if (b != null) b.style.display = DisplayStyle.None;
        }

        foreach (string reqBtn in requiredReagents)
        {
            Button b = reagentRoot.Q<Button>(reqBtn);
            if (b != null) b.style.display = DisplayStyle.Flex;
        }
    }
    // Add this new method anywhere inside the class
    public void CheckExperimentSuccess()
    {
        if (testedHCl && testedK2CrO4 && excessNaOH && heatedKI && !hasShownCustomSuccess)
        {
            hasShownCustomSuccess = true;
            ShowSuccessPopup();
        }
    }
}