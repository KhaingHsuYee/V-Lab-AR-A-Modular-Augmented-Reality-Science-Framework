using System.Collections;       
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using UnityEngine.UIElements; 
using DG.Tweening; 
// Removed UnityEngine.VFX

public class Flame_Starter_AR : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject bunsenBurnerPrefab;

    [Header("Logic Managers")]
    [SerializeField] private Flame_ExperimentManager experimentManager; 

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
    [SerializeField] private AudioClip ignitionSoundClip; 

    [Header("Shadow Settings")]
    [SerializeField] private Material shadowReceiverMaterial; // <--- NEW MATERIAL SLOT

    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool equipmentSpawned = false; 
    private GameObject spawnedBurner; 
    private Vector3 initialDropPosition; 

    void Start()
    {
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

        if (experimentUIDocument != null)
        {
            experimentUIDocument.gameObject.SetActive(true);
            var root = experimentUIDocument.rootVisualElement;
            if (root != null)
            {
                experimentScreenContainer = root.Q<VisualElement>(className: "screen-container"); 
                beginExperimentButton = root.Q<Button>("begin-button"); 

                Label subtitleLabel = root.Q<Label>("subtitle-label");
                Label inst1 = root.Q<Label>("instruction-1");
                Label inst2 = root.Q<Label>("instruction-2");
                Label inst3 = root.Q<Label>("instruction-3");

                if (subtitleLabel != null) subtitleLabel.text = "Flame Test";
                if (inst1 != null) inst1.text = "1. Select a salt sample from the menu.";
                if (inst2 != null) inst2.text = "2. Burn the sample and observe the flame color.";
                if (inst3 != null) inst3.text = "3. Identify the unknown element.";

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
                SpawnBurnerAtPosition(initialDropPosition, hitResults[0].pose.rotation);
                equipmentSpawned = true; 
            }
        }
    }

    void SpawnBurnerAtPosition(Vector3 position, Quaternion rotation)
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0; 
        Quaternion cameraLookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);

        spawnedBurner = Instantiate(bunsenBurnerPrefab, position + Vector3.up * 1.5f, cameraLookRotation);
        spawnedBurner.tag = "Burner";
        
        // Disable Particle Systems on spawn
        ParticleSystem[] flameParticles = spawnedBurner.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in flameParticles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        spawnedBurner.transform.DOMove(position, 0.6f).SetEase(Ease.OutBounce).OnComplete(() => {
            if (dropSoundClip != null)
            {
                AudioSource src = spawnedBurner.GetComponent<AudioSource>();
                if (src == null) src = spawnedBurner.AddComponent<AudioSource>();
                src.PlayOneShot(dropSoundClip);
            }
        });

        // --- NEW CODE: Swap Dotted Grid for Invisible Shadow Catcher ---
        if (planeManager != null)
        {
            planeManager.enabled = false; // Stop searching for new tables
            
            foreach (var plane in planeManager.trackables) 
            {
                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                if (meshRenderer != null && shadowReceiverMaterial != null)
                {
                    // Instantly swap the dotted pattern for the shadow material!
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
        // ----------------------------------------------------------------

        StartCoroutine(DelayExperimentUI());
    }

    private IEnumerator DelayExperimentUI()
    {
        yield return new WaitForSeconds(2f);
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
                
                if (spawnedBurner != null)
                {
                    // Enable Particle Systems on ignition
                    ParticleSystem[] flameParticles = spawnedBurner.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in flameParticles)
                    {
                        ps.Play(true);
                    }

                    if (ignitionSoundClip != null)
                    {
                        AudioSource src = spawnedBurner.GetComponent<AudioSource>();
                        if (src == null) src = spawnedBurner.AddComponent<AudioSource>();
                        src.PlayOneShot(ignitionSoundClip);
                    }
                }

                if (experimentManager != null) experimentManager.InitializeLab();
            }); 
        }
    }
}