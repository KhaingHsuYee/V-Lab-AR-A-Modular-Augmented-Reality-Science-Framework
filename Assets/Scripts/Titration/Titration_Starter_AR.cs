using UnityEngine;
using System.Collections;       
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using UnityEngine.UIElements; 
using DG.Tweening; 

public class Titration_Starter_AR : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject conicalFlaskPrefab; // Swapped from rack to flask

    [Header("Logic Managers")]
    [SerializeField] private Titration_ExperimentManager experimentManager; 

    [Header("Welcome UI (UI Toolkit)")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    private VisualElement welcomeScreenContainer; 
    private Button dismissButton;    
    
    [Header("Experiment UI (UI Toolkit)")]
    [SerializeField] private UIDocument experimentUIDocument; 
    private VisualElement experimentScreenContainer; 
    private Button beginExperimentButton; 

    [Header("Audio Settings")]
    [SerializeField] private AudioClip glassPlaceSound; 
    [SerializeField] private AudioClip liquidPourSound; // Sound for the 10mL auto-fill

    [Header("Shadow Settings")]
    [SerializeField] private Material shadowReceiverMaterial; 

    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    public bool experimentActive = false; 

    private GameObject spawnedFlask; 
    private Material flaskLiquidMaterial;

    [Header("Experiment Instructions")]
    [SerializeField] private string experimentTitle = "Acid-Base Titration";
    [SerializeField] private string instruction1 = "1. Setup the Titration apparatus.";
    [SerializeField] private string instruction2 = "2. Add Methyl Orange indicator.";
    [SerializeField] private string instruction3 = "3. Titrate until color changes.";

    void Start()
    {
        // --- 1. SETUP WELCOME SCREEN ---
        if (welcomeUIDocument != null)
        {
            welcomeUIDocument.gameObject.SetActive(true);
            var root = welcomeUIDocument.rootVisualElement;
            if (root != null)
            {
                welcomeScreenContainer = root.Q<VisualElement>("welcome-screen-container"); 
                dismissButton = root.Q<Button>("dismiss-button");

                if (welcomeScreenContainer != null) 
                {
                    welcomeScreenContainer.style.display = DisplayStyle.Flex;
                    welcomeScreenContainer.style.scale = new StyleScale(new Vector2(0, 0));
                    DOTween.To(() => Vector2.zero, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBack);
                }
                
                if (dismissButton != null) dismissButton.clicked += DismissWelcomePanel;
            }
        }

        // --- 2. SETUP EXPERIMENT SCREEN ---
        if (experimentUIDocument != null)
        {
            experimentUIDocument.gameObject.SetActive(true);
            var root = experimentUIDocument.rootVisualElement;
            if (root != null)
            {
                experimentScreenContainer = root.Q<VisualElement>(className: "screen-container"); 
                beginExperimentButton = root.Q<Button>("begin-button"); 

                // --- NEW: Dynamically swap the text from the Inspector! ---
                Label subtitleLabel = root.Q<Label>("subtitle-label");
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                Label inst3 = root.Q<Label>("instruction-3");

                if (subtitleLabel != null) subtitleLabel.text = experimentTitle;
                if (inst1 != null) inst1.text = instruction1;
                if (inst2 != null) inst2.text = instruction2;
                if (inst3 != null) inst3.text = instruction3;
                // -----------------------------------------------------

                if (experimentScreenContainer != null)
                {
                    experimentScreenContainer.style.display = DisplayStyle.None;
                    experimentScreenContainer.style.scale = new StyleScale(new Vector2(0, 0));
                }
                
                if (beginExperimentButton != null) beginExperimentButton.clicked += StartExperiment;
            }
        }
    }

    private void DismissWelcomePanel()
    {
        if (welcomeScreenContainer != null)
        {
            DOTween.To(() => Vector2.one, x => welcomeScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                welcomeScreenContainer.style.display = DisplayStyle.None;
            });
        }
    }

    void Update()
    {
        if (welcomeScreenContainer != null && welcomeScreenContainer.style.display == DisplayStyle.Flex) return;
        if (experimentScreenContainer != null && experimentScreenContainer.style.display == DisplayStyle.Flex) return;

        Vector2 inputPosition = Vector2.zero;
        bool inputDetected = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            inputDetected = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPosition = Mouse.current.position.ReadValue();
            inputDetected = true;
        }

        if (inputDetected && !equipmentSpawned)
        {
            if (raycastManager.Raycast(inputPosition, hitResults, TrackableType.PlaneWithinPolygon))
            {
                SpawnFlaskAtPosition(hitResults[0].pose.position, hitResults[0].pose.rotation);
                equipmentSpawned = true; 
            }
        }
    }

    void SpawnFlaskAtPosition(Vector3 position, Quaternion rotation)
    {
        // 1. Keep the flask facing the camera
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);

        // 2. Instantiate the Flask
        spawnedFlask = Instantiate(conicalFlaskPrefab, position, cameraLookRotation);
        PlayAudio(spawnedFlask, glassPlaceSound, 0.6f);

        // 3. Find the liquid mesh robustly, no matter how deep it is nested
        Transform liquidMesh = null;
        foreach (Transform child in spawnedFlask.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Conical_flask_250ml_liquid_mesh") 
            {
                liquidMesh = child;
                break;
            }
        }

        // 4. Setup the initial liquid state (Colorless NaOH, 0 fill)
        if (liquidMesh != null)
        {
            flaskLiquidMaterial = liquidMesh.GetComponent<Renderer>().material;
            
            // Set initial state: Empty (0) and Colorless (NaOH only, no indicator yet)
            flaskLiquidMaterial.SetFloat("_FillLevel", 0f);
            
            // A highly transparent, slightly glassy color for clear NaOH
            Color colorlessLiquid = new Color(0.9f, 0.95f, 1.0f, 0.1f); 
            
            flaskLiquidMaterial.SetColor("_BaseColor", colorlessLiquid); 
            flaskLiquidMaterial.SetColor("_IndicatorColor", colorlessLiquid); 
            flaskLiquidMaterial.SetFloat("_TitrationProgress", 0f);
        }
        else
        {
            Debug.LogError("Could not find 'Conical_flask_250ml_liquid_mesh' child inside the conical flask prefab!");
        }

        // 5. Hide AR Grid Lines
        if (planeManager != null)
        {
            planeManager.enabled = false; 
            foreach (var plane in planeManager.trackables)
            {
                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                if (meshRenderer != null && shadowReceiverMaterial != null)
                    meshRenderer.material = shadowReceiverMaterial;
                
                LineRenderer line = plane.GetComponent<LineRenderer>();
                if (line != null) line.enabled = false;
            }
        }

        StartCoroutine(DelayExperimentUI());
    }

    private IEnumerator DelayExperimentUI()
    {
        yield return new WaitForSeconds(2f); // Shorter delay since there's no rack animation needed
        AnimateExperimentUIPopIn();
    }

    void AnimateExperimentUIPopIn()
    {
        if (experimentScreenContainer != null)
        {
            experimentScreenContainer.style.display = DisplayStyle.Flex;
            DOTween.To(() => Vector2.zero, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.one, 0.5f).SetEase(Ease.OutBack); 
        }
    }

    void StartExperiment()
    {
        if (experimentScreenContainer != null)
        {
            DOTween.To(() => Vector2.one, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    experimentScreenContainer.style.display = DisplayStyle.None;
                    AnimateFlaskFill(); // Trigger the 10mL fill animation!
            }); 
        }
    }

    void AnimateFlaskFill()
    {
        experimentActive = true; 

        // Play the pouring sound and force it to stop after 3.0 seconds
        if (liquidPourSound != null && spawnedFlask != null)
        {
            PlayAudio(spawnedFlask, liquidPourSound, 0.8f, 1.0f);
        }

        // Animate the shader fill level from 0 to 0.2 (which represents 10mL in a 50mL flask)
        if (flaskLiquidMaterial != null)
        {
            flaskLiquidMaterial.DOFloat(0.2f, "_FillLevel", 2.0f).SetEase(Ease.InOutSine).OnComplete(() => 
            {
                // Once the flask is full, initialize the main experiment UI and logic
                if (experimentManager != null)
                {
                    experimentManager.InitializeLab(spawnedFlask, flaskLiquidMaterial);
                }
            });
        }
    }

    void PlayAudio(GameObject targetSpawn, AudioClip clipToPlay, float volume, float stopAfterSeconds = 0f)
    {
        if (clipToPlay == null) return;
        AudioSource audioSource = targetSpawn.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = targetSpawn.AddComponent<AudioSource>();
        audioSource.clip = clipToPlay;
        audioSource.spatialBlend = 1.0f; 
        audioSource.volume = volume;
        audioSource.Play();

        // NEW: Automatically stop the sound after a set time
        if (stopAfterSeconds > 0f)
        {
            DOVirtual.DelayedCall(stopAfterSeconds, () => {
                // Check if the source still exists, is playing, and hasn't been hijacked by a new sound
                if (audioSource != null && audioSource.isPlaying && audioSource.clip == clipToPlay)
                {
                    audioSource.Stop();
                }
            });
        }
    }
}