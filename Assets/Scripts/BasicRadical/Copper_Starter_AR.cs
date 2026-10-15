using UnityEngine;
using System.Collections;       
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using UnityEngine.UIElements; 
using DG.Tweening; 

public class Copper_Starter_AR : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject testTubeRackPrefab;

    [Header("Logic Managers")]
    [SerializeField] private Copper_Rd_ExperimentManager experimentManager; 

    [Header("Welcome UI (UI Toolkit)")]
    [SerializeField] private UIDocument welcomeUIDocument; 
    private VisualElement welcomeScreenContainer; 
    private Button dismissButton;    
    
    [Header("Experiment UI (UI Toolkit)")]
    [SerializeField] private UIDocument experimentUIDocument; 
    private VisualElement experimentScreenContainer; 
    private Button beginExperimentButton; 

    [Header("Audio Settings")]
    [SerializeField] private AudioClip rackDropSound; 

    [Header("Shadow Settings")]
    [SerializeField] private Material shadowReceiverMaterial; // <--- NEW MATERIAL SLOT

    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    public bool experimentActive = false; 

    private GameObject spawnedRack; 
    private Vector3 initialDropPosition; 

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
                SpawnRackAtPosition(initialDropPosition, hitResults[0].pose.rotation);
                equipmentSpawned = true; 
            }
        }
    }

    void SpawnRackAtPosition(Vector3 position, Quaternion rotation)
    {
        // 1. The missing math that calculates 'correctedRotation'
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
        Quaternion customOffset = Quaternion.Euler(-90f, 0f, 0f);
        Quaternion correctedRotation = cameraLookRotation * customOffset;

        // 2. Instantiate the rack
        spawnedRack = Instantiate(testTubeRackPrefab, position, correctedRotation);
        PlayDropAudio(spawnedRack, rackDropSound, 0.6f);

        // 3. Link the VISUAL tubes to the controller (Fixes the dropper position)
        var reactionController = FindObjectOfType<Copper_Rd_ChemicalReactionController>();
        if (reactionController != null)
        {
            for (int i = 0; i < 6; i++)
            {
                string tubePath = "Empty_R1_C" + (i + 1) + "/Tube_R1_C" + (i + 1);
                Transform visualTube = spawnedRack.transform.Find(tubePath);
                
                if (visualTube != null)
                {
                    reactionController.tubeParents[i] = visualTube;
                }
                else
                {
                    Debug.LogError("Could not find visual tube at path: " + tubePath);
                }
            }
        }

        // --- NEW CODE: Swap Dotted Grid for Invisible Shadow Catcher ---
        if (planeManager != null)
        {
            planeManager.enabled = false; // Stop searching for new tables so the grid doesn't keep generating

            foreach (var plane in planeManager.trackables)
            {
                // Swap the dotted grid out for the invisible shadow material
                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                if (meshRenderer != null && shadowReceiverMaterial != null)
                {
                    meshRenderer.material = shadowReceiverMaterial;
                }
                
                // Turn off the glowing border if your prefab uses it
                LineRenderer line = plane.GetComponent<LineRenderer>();
                if (line != null) 
                {
                    line.enabled = false;
                }
            }
        }
        // -------------------------------------

        StartCoroutine(DelayExperimentUI());
    }

    private IEnumerator DelayExperimentUI()
    {
        yield return new WaitForSeconds(5f);
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
            DOTween.To(() => Vector2.one, x => experimentScreenContainer.style.scale = new StyleScale(x), Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                experimentScreenContainer.style.display = DisplayStyle.None;
                AnimateLaboratorySetup();
            }); 
        }
    }

    
    
    // --- UPDATED ANIMATION METHOD ---
    void AnimateLaboratorySetup()
    {
        experimentActive = true; 

        if (experimentManager != null)
        {
            experimentManager.InitializeLab();
        }

        if (spawnedRack != null)
        {
            Vector3 rackOffset = Camera.main.transform.right * -0.25f; 
            Vector3 targetRackPos = initialDropPosition + rackOffset;
            Transform targetTube = spawnedRack.transform.Find("Empty_R1_C1/Tube_R1_C1");
            
            if (targetTube != null)
            {
                Sequence labSetupSequence = DOTween.Sequence();

                // Step 1: Slide the WHOLE rack to the left
                labSetupSequence.Append(spawnedRack.transform.DOMove(targetRackPos, 1.5f).SetEase(Ease.InOutQuad));

                // Step 2: LIFT STRAIGHT UP (Uses SetRelative to force it upward in World Space)
                labSetupSequence.Append(targetTube.DOMoveY(0.25f, 0.4f).SetRelative().SetEase(Ease.OutSine));

                // Step 3: MOVE ACROSS (To a position 25cm directly above the final table spot)
                labSetupSequence.Append(targetTube.DOMove(initialDropPosition + (Vector3.up * 0.25f), 0.6f).SetEase(Ease.InOutSine));

                // Step 4: MOVE STRAIGHT DOWN (Onto the table)
                labSetupSequence.Append(targetTube.DOMove(initialDropPosition, 0.4f).SetEase(Ease.InSine));

                var reactionController = FindObjectOfType<Copper_Rd_ChemicalReactionController>();
                if (reactionController != null)
                {
                    reactionController.testingPosition = initialDropPosition;
                }
            }
            else
            {
                Debug.LogError("Could not find 'Empty_R1_C1/Tube_R1_C1' in the spawned rack!");
            }
        }
    }

    void PlayDropAudio(GameObject targetSpawn, AudioClip clipToPlay, float volume)
    {
        if (clipToPlay == null) return;
        AudioSource audioSource = targetSpawn.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = targetSpawn.AddComponent<AudioSource>();
        audioSource.clip = clipToPlay;
        audioSource.spatialBlend = 1.0f; 
        audioSource.volume = volume;
        audioSource.Play();
    }
}