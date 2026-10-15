using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Vinegar_ExperimentManager : MonoBehaviour
{
    [Header("Results UI Integration")]
    [SerializeField] private UIDocument resultsUIDocument;
    [SerializeField] private Texture2D resultsIcon;
    
    // UI Elements for Results
    private VisualElement resultsOverlay;
    private TextField inputTrial1;
    private TextField inputTrial2;
    private TextField inputTrial3;
    private TextField inputConcentration;
    private TextField inputGrams;       // NEW: For 4.80 g
    private TextField inputPercentage;  // NEW: For 4.8%
    private Label meanResultText;
    private Label feedbackText;
    private Button btnCalcMean;
    private Button btnCloseResults;
    private Button btnSubmitResults;
    private float calculatedMeanVolume = 0f;

    [Header("UI References")]
    [SerializeField] private UIDocument mainHudUIDocument;
    [SerializeField] private UIDocument titrationUIDocument;

    [Header("Burette Controls UI")]
    private Button btnResetBurette;

    [Header("Apparatus Settings")]
    [SerializeField] private GameObject titrationStandPrefab;
    [SerializeField] private float flaskDockingYOffset = 0.05f;

    [Header("Volume & Fill Settings")]
    [SerializeField] private float buretteMaxVolume = 25.0f;
    [SerializeField] private float flaskMaxVolume = 50.0f;
    [SerializeField] private float initialFlaskVolume = 10.0f;
    [SerializeField] private float flaskFillOffset = 0.03f;

    [Header("Chemistry Setup (VINEGAR LAB)")]
    [SerializeField] private float molarityBase = 0.10f; // 0.10 M NaOH

    // NEW: Array of possible unknown concentrations
    private float[] possibleMolarities = new float[] { 0.05f, 0.08f, 0.10f }; 
    private float molarityAcid; 
    private float targetEndpointVolume;
    private enum TitrationState { Basic, Endpoint, Acidic }
    private TitrationState currentState = TitrationState.Basic;

    [Header("Particle Physics Tuning")]
    [SerializeField] private float baseLiquidHeight = -0.03f; 
    [SerializeField] private float heightRisePerML = 0.00f;
    [SerializeField] private float maxKillZoneHeight = 0.1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource pourAudioSource;
    [SerializeField] private AudioClip continuousPourSound;
    [SerializeField] private AudioClip dripSound;
    [SerializeField] private AudioClip standAppearSound;
    [SerializeField] private AudioClip flaskDockSound;

    [Header("Animation Polish Settings")]
    [Tooltip("How aggressively the liquid swirls. Try 0.2 to 1.0")]
    [SerializeField] private float maxWobbleIntensity = 0.5f; 
    private float currentWobbleAmplitude = 0f;

    [Header("Dropper Animation Settings")]
    [SerializeField] private GameObject dropperPrefab; 
    [SerializeField] private GameObject dropPrefab; 
    [SerializeField] private Vector3 dropperScale = new Vector3(0.02f, 0.02f, 0.02f);
    [SerializeField] private Vector3 dropperRotation = new Vector3(-90f, 0f, 0f);

    private GameObject activeFlask;
    private Material flaskLiquidMaterial;
    private Material buretteLiquidMaterial;
    
    private ParticleSystem acidFlowParticles;
    private ParticleSystem reactionBloomParticles;
    private Transform killZoneTransform;

    private VisualElement hudContainer;
    private Label statusTextLeft;
    private Button btnHome;
    private Button btnReset;
    private Button btnAdd;

    private Slider buretteSlider;
    private Label volumeText;
    private Label valveStatusText;

    private bool isTitrating = false;
    private float dispensedVolume = 0.00f;
    private float maxPourRate = 3.0f; 

    private VisualElement exitPopupOverlay;
    private VisualElement exitPopupBox;
    private Label popupTitle;        
    private Label popupMessage;      
    private Button btnCancelExit;
    private Button btnConfirmExit;

    [Header("Success UI")]
    private VisualElement successOverlay;
    private VisualElement successBox;
    private Button btnStaySuccess;
    private Button btnExitSuccess;

    [Header("3D Hologram Dashboard")]
    public GameObject dashboardGameObject; 
    public float dashboardHeightOffset = 0.4f;

    private enum PopupState { None, Home, Reset }
    private PopupState currentPopupState = PopupState.None;

    public void InitializeLab(GameObject flask, Material liquidMat)
    {
        activeFlask = flask;
        flaskLiquidMaterial = liquidMat;

        // Randomly pick the unknown concentration for this session
        molarityAcid = possibleMolarities[Random.Range(0, possibleMolarities.Length)];
        
        // Calculate the exact volume needed to neutralize the base (M1V1 = M2V2)
        targetEndpointVolume = (initialFlaskVolume * molarityBase) / molarityAcid;
        
        // Cheat sheet for testing in the Unity Console
        Debug.Log($"[Cheat Sheet] Selected Molarity: {molarityAcid} M | Target Volume: {targetEndpointVolume} mL");

        if (pourAudioSource != null) pourAudioSource.loop = true;

        if (flaskLiquidMaterial != null)
        {
            float initialFill = (initialFlaskVolume / flaskMaxVolume) + flaskFillOffset;
            flaskLiquidMaterial.SetFloat("_FillLevel", initialFill);
            flaskLiquidMaterial.SetFloat("_WobbleIntensity", 0f);
        }

        if (mainHudUIDocument != null)
        {
            mainHudUIDocument.gameObject.SetActive(true);
            var root = mainHudUIDocument.rootVisualElement;
            if (root != null)
            {
                hudContainer = root.Q<VisualElement>("hud-container");
                statusTextLeft = root.Q<Label>("status-text-left");
                btnHome = root.Q<Button>("btn-home");
                btnReset = root.Q<Button>("btn-reset");
                btnAdd = root.Q<Button>("btn-add");

                exitPopupOverlay = root.Q<VisualElement>("exit-popup-overlay");
                exitPopupBox = root.Q<VisualElement>("exit-popup-box");
                popupTitle = root.Q<Label>("popup-title");
                popupMessage = root.Q<Label>("popup-message");
                btnCancelExit = root.Q<Button>("btn-cancel-exit");
                btnConfirmExit = root.Q<Button>("btn-confirm-exit");

                if (btnHome != null) btnHome.clicked += PromptGoHome;
                if (btnReset != null) btnReset.clicked += PromptReset;
                if (btnCancelExit != null) btnCancelExit.clicked += ClosePopup;
                if (btnConfirmExit != null) btnConfirmExit.clicked += ConfirmPopupAction;

                var btnReagent = root.Q<Button>("btn-reagent");
                if (btnReagent != null)
                {
                    btnReagent.clicked += OpenResultsUI;
                    if (resultsIcon != null)
                    {
                        btnReagent.style.backgroundImage = new StyleBackground(resultsIcon);
                    }
                }

                if (btnAdd != null)
                {
                    btnAdd.text = "ADD\nPHENOLPHTHALEIN"; 
                    
                    btnAdd.style.width = 175;
                    btnAdd.style.height = 65;
                    btnAdd.style.fontSize = 13;
                    btnAdd.style.whiteSpace = WhiteSpace.Normal; 

                    btnAdd.style.display = DisplayStyle.Flex;
                    DOTween.To(() => new Vector2(1, 1), x => btnAdd.style.scale = new StyleScale(new Vector2(x.x, x.y)), new Vector2(1.05f, 1.05f), 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    btnAdd.clicked += OnAddIndicatorClicked;
                }

                UpdateStatusText("Action Required: Add Phenolphthalein indicator to the NaOH solution.");
                if (hudContainer != null)
                {
                    hudContainer.style.display = DisplayStyle.Flex;
                    hudContainer.style.opacity = 0f;
                    DOTween.To(() => 0f, x => hudContainer.style.opacity = x, 1f, 1f);
                }
            }
        }

        if (resultsUIDocument != null)
        {
            resultsUIDocument.gameObject.SetActive(true);
            var resRoot = resultsUIDocument.rootVisualElement;
            if (resRoot != null)
            {
                resultsOverlay = resRoot.Q<VisualElement>("results-overlay");
                if (resultsOverlay != null) resultsOverlay.style.display = DisplayStyle.None; 

                inputTrial1 = resRoot.Q<TextField>("input-trial-1");
                inputTrial2 = resRoot.Q<TextField>("input-trial-2");
                inputTrial3 = resRoot.Q<TextField>("input-trial-3");
                
                inputConcentration = resRoot.Q<TextField>("input-concentration");
                inputGrams = resRoot.Q<TextField>("input-grams");
                inputPercentage = resRoot.Q<TextField>("input-percentage");
                
                meanResultText = resRoot.Q<Label>("mean-result-text");
                feedbackText = resRoot.Q<Label>("feedback-text");
                
                btnCalcMean = resRoot.Q<Button>("btn-calc-mean");
                btnCloseResults = resRoot.Q<Button>("btn-close-results");
                btnSubmitResults = resRoot.Q<Button>("btn-submit-results");

                if (btnCalcMean != null) btnCalcMean.clicked += CalculateMean;
                if (btnCloseResults != null) btnCloseResults.clicked += CloseResultsUI;
                if (btnSubmitResults != null) btnSubmitResults.clicked += SubmitFinalAnswer;

                // --- NEW: Bind Success Popup Elements ---
                successOverlay = resRoot.Q<VisualElement>("success-overlay");
                successBox = resRoot.Q<VisualElement>("success-box");
                btnStaySuccess = resRoot.Q<Button>("btn-stay-success");
                btnExitSuccess = resRoot.Q<Button>("btn-exit-success");

                if (successOverlay != null) successOverlay.style.display = DisplayStyle.None;
                
                if (btnStaySuccess != null) btnStaySuccess.clicked += HideSuccessPopup;
                if (btnExitSuccess != null) btnExitSuccess.clicked += () => { SceneManager.LoadScene("Chemistry_Home"); };
            }
        }

        if (dashboardGameObject != null && activeFlask != null)
        {
            Vector3 spawnOffset = (Vector3.up * dashboardHeightOffset) + (Camera.main.transform.forward * 0.1f);
            dashboardGameObject.transform.position = activeFlask.transform.position + spawnOffset;

            if (Camera.main != null)
            {
                Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                dashboardGameObject.transform.LookAt(targetPosition);
                dashboardGameObject.transform.Rotate(0, 180, 0); 
            }

            dashboardGameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (isTitrating && buretteSlider != null && flaskLiquidMaterial != null)
        {
            float flowRate = buretteSlider.value; 

            if (flowRate > 0)
            {
                dispensedVolume += flowRate * maxPourRate * Time.deltaTime;
                
                if (dispensedVolume >= buretteMaxVolume)
                {
                    dispensedVolume = buretteMaxVolume; 
                    flowRate = 0f; 
                    
                    buretteSlider.value = 0f;
                    buretteSlider.SetEnabled(false);
                    
                    if (valveStatusText != null)
                    {
                        valveStatusText.text = "BURETTE EMPTY";
                        valveStatusText.style.color = new StyleColor(ColorUtility.TryParseHtmlString("#ef4444", out Color c) ? c : Color.red);
                    }
                    UpdateStatusText("The burette is empty! You must reset the experiment.");
                }

                if (volumeText != null) volumeText.text = dispensedVolume.ToString("F2");

                float currentFlaskVol = initialFlaskVolume + dispensedVolume;
                float flaskFill = (currentFlaskVol / flaskMaxVolume) + flaskFillOffset;
                flaskLiquidMaterial.SetFloat("_FillLevel", Mathf.Clamp01(flaskFill));

                if (buretteLiquidMaterial != null)
                {
                    float buretteFill = (buretteMaxVolume - dispensedVolume) / buretteMaxVolume;
                    buretteLiquidMaterial.SetFloat("_FillLevel", Mathf.Clamp01(buretteFill));
                }

                // --- DYNAMIC pH ENGINE (VINEGAR / WEAK ACID UPDATE) ---
                float molesBase_mmol = initialFlaskVolume * molarityBase;
                float molesAcid_mmol = dispensedVolume * molarityAcid;
                float currentPH = 7.0f;
                float pKa = 4.74f; // The pKa constant of Acetic Acid

                if (molesBase_mmol > molesAcid_mmol) 
                {
                    // 1. Strong Base is in excess. The OH- dominates the pH.
                    float remainingBase = molesBase_mmol - molesAcid_mmol;
                    float concentrationOH = remainingBase / currentFlaskVol;
                    currentPH = 14.0f + Mathf.Log10(concentrationOH);
                }
                else if (molesAcid_mmol > molesBase_mmol) 
                {
                    // 2. WEAK ACID BUFFER REGION (Henderson-Hasselbalch equation)
                    // The flask contains CH3COONa (salt) and excess CH3COOH (weak acid)
                    float excessAcid = molesAcid_mmol - molesBase_mmol;
                    float molesSalt = molesBase_mmol; // All base turned into salt
                    
                    currentPH = pKa + Mathf.Log10(molesSalt / excessAcid);
                }
                else 
                {
                    // 3. Exact equivalence point (Salt hydrolysis of weak acid/strong base)
                    currentPH = 8.72f; 
                }

                // STRICT SCIENTIFIC PRECISION: 
                // Phenolphthalein transitions exactly between pH 8.2 (Colorless) and 10.0 (Pink).
                float indicatorLerp = Mathf.InverseLerp(8.2f, 10.0f, currentPH);
                
                // We add a SmoothStep easing function to make the visual transition 
                // snap dramatically drop-by-drop through the steep part of the curve!
                indicatorLerp = Mathf.SmoothStep(0.0f, 1.0f, indicatorLerp);
                
                Color colorless = new Color(0.9f, 0.95f, 1.0f, 0.15f); // Slightly glassy
                Color deepPink = new Color(1.0f, 0.0f, 0.5f, 0.85f);   // Strong Magenta/Pink
                
                flaskLiquidMaterial.SetColor("_BaseColor", Color.Lerp(colorless, deepPink, indicatorLerp));

                if (dispensedVolume < targetEndpointVolume - 0.2f && currentState != TitrationState.Basic)
                {
                    currentState = TitrationState.Basic;
                    UpdateStatusText("Solution is basic (Pink). Watch for the color transition!");
                }
                else if (dispensedVolume >= targetEndpointVolume - 0.2f && dispensedVolume <= targetEndpointVolume + 0.2f && currentState != TitrationState.Endpoint)
                {
                    currentState = TitrationState.Endpoint;
                    UpdateStatusText("Endpoint reached! The solution is turning pale pink. Close the valve!");
                }
                else if (dispensedVolume > targetEndpointVolume + 0.2f && currentState != TitrationState.Acidic)
                {
                    currentState = TitrationState.Acidic;
                    UpdateStatusText("Overshot! The solution is highly acidic (Colorless). You must reset.");
                }
            }

            if (pourAudioSource != null && continuousPourSound != null)
            {
                if (flowRate > 0)
                {
                    if (pourAudioSource.clip != continuousPourSound) pourAudioSource.clip = continuousPourSound;
                    if (!pourAudioSource.isPlaying) pourAudioSource.Play();
                    
                    pourAudioSource.volume = Mathf.Lerp(0.0f, 1.0f, flowRate);
                    pourAudioSource.pitch = Mathf.Lerp(0.7f, 1.3f, flowRate);
                }
                else
                {
                    if (pourAudioSource.isPlaying) pourAudioSource.Pause();
                }
            }

            float targetWobble = flowRate * maxWobbleIntensity;
            currentWobbleAmplitude = Mathf.Lerp(currentWobbleAmplitude, targetWobble, Time.deltaTime * 5f);
            flaskLiquidMaterial.SetFloat("_WobbleIntensity", currentWobbleAmplitude);

            if (acidFlowParticles != null)
            {
                var emission = acidFlowParticles.emission;
                if (flowRate > 0)
                {
                    if (!acidFlowParticles.isPlaying) acidFlowParticles.Play();
                    emission.rateOverTime = flowRate * 350f; 
                }
                else
                {
                    emission.rateOverTime = 0f;
                    if (acidFlowParticles.isPlaying) acidFlowParticles.Stop();
                }
            }

            if (reactionBloomParticles != null)
            {
                var bloomEmission = reactionBloomParticles.emission;
                if (flowRate > 0)
                {
                    if (!reactionBloomParticles.isPlaying) reactionBloomParticles.Play();
                    bloomEmission.rateOverTime = 5f + (flowRate * 20f); 
                }
                else
                {
                    bloomEmission.rateOverTime = 0f;
                    if (reactionBloomParticles.isPlaying) reactionBloomParticles.Stop();
                }
            }

            if (flowRate > 0 && killZoneTransform != null && activeFlask != null)
            {
                float calculatedRise = dispensedVolume * heightRisePerML;
                float currentLiquidHeight = baseLiquidHeight + calculatedRise;
                currentLiquidHeight = Mathf.Clamp(currentLiquidHeight, baseLiquidHeight, maxKillZoneHeight);
                killZoneTransform.position = activeFlask.transform.position + new Vector3(0, currentLiquidHeight, 0);
            }
        }
    }

    private void OnAddIndicatorClicked()
    {
        if (btnAdd != null) { btnAdd.style.display = DisplayStyle.None; DOTween.Kill(btnAdd); }
        UpdateStatusText("Adding Phenolphthalein...");
        PlayDropperAnimation();
    }

    private void PlayDropperAnimation()
    {
        if (dropperPrefab == null || activeFlask == null) return;
        Vector3 flaskPos = activeFlask.transform.position;
        Vector3 startPos = flaskPos + (Vector3.up * 0.45f); 
        Vector3 lowerPos = flaskPos + (Vector3.up * 0.20f);

        GameObject dropper = Instantiate(dropperPrefab, startPos, Quaternion.Euler(dropperRotation));
        dropper.SetActive(true); 
        dropper.transform.localScale = dropperScale; 

        Material dropperLiquidMat = null;
        Renderer[] renderers = dropper.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.name.ToLower().Contains("liquid") || r.sharedMaterial.name.ToLower().Contains("liquid"))
            {
                dropperLiquidMat = r.material;
                // NEW: Dropper liquid is colorless (Phenolphthalein dissolved in alcohol)
                dropperLiquidMat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f, 0.3f));
                dropperLiquidMat.SetFloat("_FillLevel", 0.8f);
                break;
            }
        }

        Sequence dropSeq = DOTween.Sequence();
        dropSeq.Append(dropper.transform.DOMove(lowerPos, 0.5f).SetEase(Ease.OutBack));

        float currentFill = 0.8f;
        float fillDecrement = 0.8f / 3f; 
        
        // NEW: Flask starts colorless, transitions to deep pink when indicator is added to NaOH
        Color startingFlaskColor = new Color(0.9f, 0.95f, 1.0f, 0.15f);
        Color deepPinkIndicator = new Color(1.0f, 0.0f, 0.5f, 0.85f);

        for (int i = 0; i < 3; i++)
        {
            int currentDropIndex = i + 1; 
            dropSeq.AppendInterval(0.4f);
            dropSeq.AppendCallback(() => {
                if (sfxSource != null && dripSound != null) sfxSource.PlayOneShot(dripSound);
                if (dropperLiquidMat != null)
                {
                    currentFill -= fillDecrement;
                    if (currentDropIndex == 3) currentFill = 0f; 
                    dropperLiquidMat.DOFloat(currentFill, "_FillLevel", 0.2f);
                }
                if (dropPrefab != null)
                {
                    Vector3 tipPos = dropper.transform.position + (Vector3.down * 0.05f);
                    GameObject drop = Instantiate(dropPrefab, tipPos, Quaternion.identity);
                    drop.transform.localScale = Vector3.zero;

                    Sequence singleDropSeq = DOTween.Sequence();
                    singleDropSeq.Append(drop.transform.DOScale(new Vector3(0.015f, 0.015f, 0.015f), 0.1f)); 
                    singleDropSeq.Append(drop.transform.DOMove(flaskPos, 0.3f).SetEase(Ease.InCubic)); 
                    singleDropSeq.OnComplete(() => {
                        Destroy(drop);
                        if (flaskLiquidMaterial != null)
                        {
                            Color targetColor = Color.Lerp(startingFlaskColor, deepPinkIndicator, currentDropIndex / 3f);
                            flaskLiquidMaterial.DOColor(targetColor, "_BaseColor", 0.4f);
                        }
                    });
                }
            });
        }
        dropSeq.AppendInterval(0.5f);
        dropSeq.Append(dropper.transform.DOMove(startPos, 0.4f).SetEase(Ease.InSine));
        dropSeq.OnComplete(() => { Destroy(dropper); ShowSetupApparatusButton(); });
    }

    private void ShowSetupApparatusButton()
    {
        UpdateStatusText("Indicator added. Ready to setup the apparatus.");
        if (btnAdd != null)
        {
            btnAdd.text = "SET UP"; 
            
            btnAdd.style.width = StyleKeyword.Null;
            btnAdd.style.height = StyleKeyword.Null;
            btnAdd.style.fontSize = StyleKeyword.Null;
            btnAdd.style.whiteSpace = StyleKeyword.Null;

            btnAdd.clicked -= OnAddIndicatorClicked; 
            btnAdd.clicked += OnSetupApparatusClicked;
            btnAdd.style.display = DisplayStyle.Flex;
            DOTween.To(() => new Vector2(1, 1), x => btnAdd.style.scale = new StyleScale(new Vector2(x.x, x.y)), new Vector2(1.05f, 1.05f), 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void OnSetupApparatusClicked()
    {
        if (btnAdd != null) { btnAdd.style.display = DisplayStyle.None; DOTween.Kill(btnAdd); }
        UpdateStatusText("Setting up Titration Stand...");
        SpawnAndDockApparatus();
    }

    private void SpawnAndDockApparatus()
    {
        if (titrationStandPrefab == null || activeFlask == null) return;

        Vector3 safeSpawnPos = activeFlask.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward; cameraForward.y = 0; cameraForward.Normalize();
        Vector3 cameraRight = Camera.main.transform.right; cameraRight.y = 0; cameraRight.Normalize();
        Vector3 waitingPos = safeSpawnPos + (cameraRight * 0.2f);

        Sequence setupSeq = DOTween.Sequence();
        setupSeq.Append(activeFlask.transform.DOMove(waitingPos, 0.5f).SetEase(Ease.InOutSine));

        GameObject stand = Instantiate(titrationStandPrefab, safeSpawnPos, Quaternion.LookRotation(-cameraForward, Vector3.up));
        Vector3 targetScale = stand.transform.localScale;
        
        Transform targetDock = null;
        foreach (Transform child in stand.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Flask_Target_Pos") { targetDock = child; break; }
        }

        if (targetDock != null)
        {
            Vector3 finalDockPos = targetDock.position + new Vector3(0, flaskDockingYOffset, 0);
            stand.transform.localScale = Vector3.zero;

            setupSeq.AppendCallback(() => { if (sfxSource != null && standAppearSound != null) sfxSource.PlayOneShot(standAppearSound); });
            setupSeq.Append(stand.transform.DOScale(targetScale, 0.6f).SetEase(Ease.OutBack));
            setupSeq.AppendInterval(0.1f);
            setupSeq.Append(activeFlask.transform.DOJump(finalDockPos, 0.15f, 1, 0.8f).SetEase(Ease.InOutSine));
            
            setupSeq.OnComplete(() => {
                if (sfxSource != null && flaskDockSound != null) sfxSource.PlayOneShot(flaskDockSound);
                activeFlask.transform.SetParent(targetDock, true);
                activeFlask.transform.localPosition = new Vector3(0, flaskDockingYOffset, 0);
                
                foreach (ParticleSystem ps in stand.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.gameObject.name == "Acid_Flow_Particles") acidFlowParticles = ps;
                }

                foreach (ParticleSystem ps in activeFlask.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.gameObject.name == "Reaction_Bloom_Particles") 
                    {
                        reactionBloomParticles = ps;
                        
                        // NEW: Override the particle color dynamically for this experiment only
                        var mainModule = reactionBloomParticles.main;
                        // Sets the particles to a slightly visible colorless/glassy tint
                        mainModule.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.95f, 1.0f, 0.3f)); 
                    }
                }

                foreach (Renderer r in stand.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name.ToLower().Contains("liquid") && r.material.HasProperty("_FillLevel"))
                    {
                        buretteLiquidMaterial = r.material;
                        buretteLiquidMaterial.SetFloat("_FillLevel", 1.0f);
                        break;
                    }
                }

                foreach (Transform child in stand.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Liquid_Kill_Zone")
                    {
                        killZoneTransform = child;
                        break;
                    }
                }

                if (killZoneTransform != null && acidFlowParticles != null)
                {
                    var collision = acidFlowParticles.collision;
                    collision.enabled = true;
                    collision.type = ParticleSystemCollisionType.Planes;
                    collision.SetPlane(0, killZoneTransform);
                }
                
                ShowBuretteControls();
            });
        }
    }

    private void ShowBuretteControls()
    {
        UpdateStatusText("Apparatus ready. Drag the slider up to open the valve.");

        if (titrationUIDocument != null)
        {
            titrationUIDocument.gameObject.SetActive(true);
            var root = titrationUIDocument.rootVisualElement;
            if (root != null)
            {
                buretteSlider = root.Q<Slider>("burette-slider");
                volumeText = root.Q<Label>("volume-text");
                valveStatusText = root.Q<Label>("valve-status-text");

                btnResetBurette = root.Q<Button>("btn-reset-burette");
                if (btnResetBurette != null)
                {
                    btnResetBurette.clicked += ResetTrial;
                }

                if (buretteSlider != null && valveStatusText != null)
                {
                    buretteSlider.RegisterValueChangedCallback(evt => {
                        if (dispensedVolume >= buretteMaxVolume) return;

                        if (evt.newValue > 0.05f) 
                        {
                            valveStatusText.text = "POURING";
                            valveStatusText.style.color = new StyleColor(ColorUtility.TryParseHtmlString("#86efac", out Color c) ? c : Color.green);
                        } 
                        else 
                        {
                            valveStatusText.text = "VALVE CLOSED";
                            valveStatusText.style.color = new StyleColor(ColorUtility.TryParseHtmlString("#e2e8f0", out Color c2) ? c2 : Color.white);
                        }
                    });
                }
                isTitrating = true;
            }
        }
    }

    private void OpenResultsUI()
    {
        if (resultsOverlay != null)
        {
            resultsOverlay.style.display = DisplayStyle.Flex;
            feedbackText.text = ""; 
        }

        // Hides the 3D dashboard so it doesn't overlap with the results UI
        if (dashboardGameObject != null) 
        {
            dashboardGameObject.SetActive(false);
        }
    }

    private void CloseResultsUI()
    {
        if (resultsOverlay != null) 
        {
            resultsOverlay.style.display = DisplayStyle.None;
        }

        // Restores the 3D dashboard when the results are closed
        if (dashboardGameObject != null) 
        {
            dashboardGameObject.SetActive(true);
        }
    }

    private void CalculateMean()
    {
        if (float.TryParse(inputTrial2.value, out float t2) && float.TryParse(inputTrial3.value, out float t3))
        {
            calculatedMeanVolume = (t2 + t3) / 2.0f;
            meanResultText.text = $"Mean Volume: {calculatedMeanVolume:F2} mL";
        }
        else
        {
            meanResultText.text = "Error: Enter valid numbers.";
        }
    }

    private void SubmitFinalAnswer()
    {
        bool isMolarityCorrect = false;
        bool isGramsCorrect = false;
        bool isPercentCorrect = false;

        float m = 0f, g = 0f, p = 0f;

        bool hasM = inputConcentration != null && float.TryParse(inputConcentration.value, out m);
        bool hasG = inputGrams != null && float.TryParse(inputGrams.value, out g);
        bool hasP = inputPercentage != null && float.TryParse(inputPercentage.value, out p);

        if (hasM && hasG && hasP)
        {
            // 1. Verify Molarity matches the dynamically chosen target for this session
            isMolarityCorrect = (float)System.Math.Round(m, 2) == (float)System.Math.Round(molarityAcid, 2);

            // 2. Dynamically calculate based on user's entered Molarity (m)
            float expectedGrams = m * 60.05f; 
            
            // 3. Direct conversion: g/L divided by 10 = % w/v (grams per 100mL)
            float expectedPercentage = expectedGrams / 10f; 

            // 4. Validate with a small tolerance for manual rounding
            isGramsCorrect = Mathf.Abs(g - expectedGrams) <= 0.05f;
            isPercentCorrect = Mathf.Abs(p - expectedPercentage) <= 0.05f;
        }

        if (isMolarityCorrect && isGramsCorrect && isPercentCorrect)
        {
            feedbackText.text = "SUCCESS! Your calculations are correct!";
            feedbackText.style.color = new StyleColor(Color.green);

            // --- NEW: Success Popup Sequence ---
            if (btnSubmitResults != null) btnSubmitResults.SetEnabled(false);

            DOVirtual.DelayedCall(5.0f, () => 
            {
                CloseResultsUI(); 
                ShowSuccessPopup(); 
                
                if (btnSubmitResults != null) btnSubmitResults.SetEnabled(true);
            });
        }
        else
        {
            feedbackText.text = "INCORRECT. Please recheck your math based on your calculated molarity.";
            feedbackText.style.color = new StyleColor(Color.red);
        }
    }
    private void ResetTrial()
    {
        dispensedVolume = 0f;
        currentState = TitrationState.Basic; 
        
        if (buretteSlider != null) 
        {
            buretteSlider.SetEnabled(true); 
            buretteSlider.value = 0f;
        }
        if (volumeText != null) volumeText.text = "0.00";
        if (valveStatusText != null)
        {
            valveStatusText.text = "VALVE CLOSED";
            valveStatusText.style.color = new StyleColor(ColorUtility.TryParseHtmlString("#e2e8f0", out Color c2) ? c2 : Color.white);
        }

        if (flaskLiquidMaterial != null)
        {
            float initialFill = (initialFlaskVolume / flaskMaxVolume) + flaskFillOffset;
            flaskLiquidMaterial.SetFloat("_FillLevel", initialFill);
            
            Color deepPink = new Color(1.0f, 0.0f, 0.5f, 0.85f);
            flaskLiquidMaterial.SetColor("_BaseColor", deepPink); 
        }

        // NEW: Instantly reset the burette fill level visually
        if (buretteLiquidMaterial != null)
        {
            buretteLiquidMaterial.SetFloat("_FillLevel", 1.0f);
        }

        if (pourAudioSource != null && pourAudioSource.isPlaying)
        {
            pourAudioSource.Stop();
        }

        UpdateStatusText("Burette and flask reset. Ready for the next trial.");
    }

    private void UpdateStatusText(string message) { if (statusTextLeft != null) statusTextLeft.text = message; }
    private void PromptGoHome() { currentPopupState = PopupState.Home; if (popupTitle != null) popupTitle.text = "Exit Experiment?"; if (popupMessage != null) popupMessage.text = "Are you sure you want to stop the experiment and return to the main menu?"; ShowPopup(); }
    private void PromptReset() { currentPopupState = PopupState.Reset; if (popupTitle != null) popupTitle.text = "Restart Experiment?"; if (popupMessage != null) popupMessage.text = "Are you sure you want to clear the flask and start over?"; ShowPopup(); }
    private void ShowPopup() { if (exitPopupOverlay == null || exitPopupBox == null) return; exitPopupOverlay.style.display = DisplayStyle.Flex; exitPopupBox.style.scale = new StyleScale(new Vector2(0, 0)); DOTween.To(() => Vector2.zero, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.one, 0.4f).SetEase(Ease.OutBack); }
    private void ClosePopup() { if (exitPopupOverlay == null || exitPopupBox == null) return; currentPopupState = PopupState.None; DOTween.To(() => Vector2.one, x => exitPopupBox.style.scale = new StyleScale(x), Vector2.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => { exitPopupOverlay.style.display = DisplayStyle.None; }); }
    private void ConfirmPopupAction() { if (currentPopupState == PopupState.Home) SceneManager.LoadScene("Chemistry_Home"); else if (currentPopupState == PopupState.Reset) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    private void ShowSuccessPopup()
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

}