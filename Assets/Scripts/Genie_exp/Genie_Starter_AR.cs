using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using DG.Tweening;

public class Genie_Starter_AR : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Tooltip("Add the ARAnchorManager from your XR Origin here to keep objects locked to the real world.")]
    [SerializeField] private ARAnchorManager anchorManager; 
    
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject conicalFlaskPrefab;

    [Header("Logic Managers")]
    [Tooltip("We will create Genie_ExperimentManager in the next step!")]
    [SerializeField] private MonoBehaviour experimentManager; // Placeholder for next step

    [Header("Welcome UI")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    private VisualElement welcomeScreenContainer; 
    private Button dismissButton;    
    
    [Header("Experiment UI")]
    [SerializeField] private UIDocument experimentUIDocument; 
    private VisualElement experimentScreenContainer; 
    private Button beginExperimentButton; 

    [Header("Audio")]
    [SerializeField] private AudioClip dropSoundClip; 

    [Header("Shadow Settings")]
    [SerializeField] private Material shadowReceiverMaterial;

    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    
    private GameObject anchorObject; // The real-world anchor
    private GameObject spawnedFlask; // The flask itself
    private Vector3 initialDropPosition; 

    void Start()
    {
        // 1. Setup Welcome Screen
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

        // 2. Setup Experiment Screen
        if (experimentUIDocument != null)
        {
            experimentUIDocument.gameObject.SetActive(true);
            var root = experimentUIDocument.rootVisualElement;
            if (root != null)
            {
                experimentScreenContainer = root.Q<VisualElement>(className: "screen-container"); 
                beginExperimentButton = root.Q<Button>("begin-button"); 

                // Custom UI Text for this specific experiment
                Label subtitleLabel = root.Q<Label>("subtitle-label");
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");

                if (subtitleLabel != null) subtitleLabel.text = "Genie in a Bottle";
                if (inst1 != null) inst1.text = "1. Add H2O2 from the beaker to the flask.";
                if (inst2 != null) inst2.text = "2. Add KMnO4 powder to trigger the reaction.";

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
                initialDropPosition = hitResults[0].pose.position; 
                SpawnFlaskAtAnchor(hitResults[0].pose);
                equipmentSpawned = true; 
            }
        }
    }

    void SpawnFlaskAtAnchor(Pose hitPose)
    {
        // 1. Calculate camera rotation so it faces the user
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);

        // 2. Create the AR Anchor to lock it to the real world
        anchorObject = new GameObject("Flask_AR_Anchor");
        anchorObject.transform.position = hitPose.position;
        anchorObject.transform.rotation = cameraLookRotation;
        
        if (anchorManager != null)
        {
            anchorObject.AddComponent<ARAnchor>();
        }

        // 3. Spawn the flask as a CHILD of the anchor so it doesn't float away
        spawnedFlask = Instantiate(conicalFlaskPrefab, anchorObject.transform);
        
        // Start high up for the drop animation (using Local coordinates relative to anchor)
        spawnedFlask.transform.localPosition = Vector3.up * 1.0f; 

        // Animate the drop
        spawnedFlask.transform.DOLocalMove(Vector3.zero, 0.6f).SetEase(Ease.OutBounce).OnComplete(() => {
            if (dropSoundClip != null)
            {
                AudioSource src = spawnedFlask.GetComponent<AudioSource>();
                if (src == null) src = spawnedFlask.AddComponent<AudioSource>();
                src.PlayOneShot(dropSoundClip);
            }
        });

        // 4. Swap Plane material to invisible shadow catcher
        if (planeManager != null)
        {
            planeManager.enabled = false; // Stop searching for new tables
            
            foreach (var plane in planeManager.trackables) 
            {
                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                if (meshRenderer != null && shadowReceiverMaterial != null)
                {
                    meshRenderer.material = shadowReceiverMaterial;
                }

                LineRenderer line = plane.GetComponent<LineRenderer>();
                if (line != null) line.enabled = false;
            }
        }

        StartCoroutine(DelayExperimentUI());
    }

    private IEnumerator DelayExperimentUI()
    {
        yield return new WaitForSeconds(1.5f);
        
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
            DOTween.To(() => Vector2.one, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                experimentScreenContainer.style.display = DisplayStyle.None;
                
                // Trigger the next step (We will write this manager next!)
                Debug.Log("Spawning Beaker of H2O2...");
                // experimentManager.InitializeLab(anchorObject.transform); 
            }); 
        }
    }
}