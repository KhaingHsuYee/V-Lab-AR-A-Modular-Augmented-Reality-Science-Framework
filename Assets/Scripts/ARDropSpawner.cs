using UnityEngine;
using UnityEngine.UI; // Required for the Button component
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem; 
using DG.Tweening;

public class ARDropSpawner : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject flaskPrefab;
    [SerializeField] private GameObject titrationStandPrefab;

    [Header("UI Canvas Panels")]
    [SerializeField] private GameObject welcomePanel; // Drag your Welcome Panel here
    [SerializeField] private Button dismissButton;     // Drag your Dismiss Button here

    [Header("Animation Settings")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private float airSpawnHeight = 0.45f;
    [SerializeField] private float dropTime = 0.65f;
    [SerializeField] private float spinTime = 0.5f;
    
    [Header("Titration Stand Offset")]
    [SerializeField] private Vector3 standOffset = new Vector3(-0.15f, 0, 0.05f); 

    [Header("Audio Settings")]
    [SerializeField] private AudioClip flaskDropSound;      // Drag your glass clink sound here!
    [SerializeField] private AudioClip standDropSound;      // Drag your heavy metal thud sound here!

    private List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private bool standardEquipmentSpawned = false; 

    void Awake()
    {
        // Set up the listener for the UI button right when the app starts
        if (dismissButton != null)
        {
            dismissButton.onClick.AddListener(DismissWelcomePanel);
        }
        else
        {
            Debug.LogError("Dismiss Button is unassigned in the ARDropSpawner inspector slot!");
        }
    }

    private void DismissWelcomePanel()
    {
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }
    }

    void Update()
    {
        // GUARD CLAUSE: If the welcome panel is visible, exit immediately. 
        // This stops any raycasts or screen touches from triggering by accident.
        if (welcomePanel != null && welcomePanel.activeSelf)
            return;

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

        if (inputDetected && !standardEquipmentSpawned)
        {
            if (raycastManager.Raycast(inputPosition, hitResults, TrackableType.PlaneWithinPolygon))
            {
                Pose surfacePose = hitResults[0].pose;
                Vector3 surfacePosition = surfacePose.position;

                TriggerSequentialDropSequence(surfacePosition);
                standardEquipmentSpawned = true; 
            }
        }
    }

    void TriggerSequentialDropSequence(Vector3 finalFlaskTarget)
    {
        Vector3 flaskAirStart = finalFlaskTarget + new Vector3(0, airSpawnHeight, 0);
        GameObject clonedFlask = Instantiate(flaskPrefab, flaskAirStart, Quaternion.identity);

        MeshRenderer flaskMeshRenderer = clonedFlask.GetComponentInChildren<MeshRenderer>();

        if (flaskMeshRenderer != null)
        {
            Transform flaskMesh = flaskMeshRenderer.transform;
            flaskMesh.localRotation = Quaternion.Euler(0, 0, -90f); 

            flaskMesh.DOLocalRotate(Vector3.zero, spinTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

            clonedFlask.transform.DOMove(finalFlaskTarget, dropTime)
                .SetEase(Ease.OutBounce)
                .OnComplete(() => {
                    
                    // 1. Play FLASK specific audio asset
                    PlayDropAudio(clonedFlask, flaskDropSound, 0.6f);

                    TriggerTitrationStandDrop(finalFlaskTarget);
                });
        }
        else
        {
            Debug.LogError("Could not find a MeshRenderer on the Flask Prefab structure!");
        }
    }

    void TriggerTitrationStandDrop(Vector3 flaskTargetPosition)
    {
        if (titrationStandPrefab == null)
        {
            Debug.LogWarning("Titration Stand Prefab is missing from the Inspector!");
            return;
        }

        Vector3 finalStandTarget = flaskTargetPosition + standOffset;
        Vector3 standAirStart = finalStandTarget + new Vector3(0, airSpawnHeight, 0);

        GameObject clonedStand = Instantiate(titrationStandPrefab, standAirStart, Quaternion.identity);
        MeshRenderer standMeshRenderer = clonedStand.GetComponentInChildren<MeshRenderer>();

        if (standMeshRenderer != null)
        {
            clonedStand.transform.DOMove(finalStandTarget, dropTime)
                .SetEase(Ease.OutCubic) 
                .OnComplete(() => {
                    
                    // FIXED: Passed the stand sound clip and the volume argument!
                    PlayDropAudio(clonedStand, standDropSound, 0.7f); 
                    
                    // --- CLEAN UP THE AR SCENE ---
                    if (planeManager != null)
                    {
                        planeManager.enabled = false;
                        
                        foreach (var plane in planeManager.trackables)
                        {
                            plane.gameObject.SetActive(false);
                        }
                    }
                });
        }
    }

    void PlayDropAudio(GameObject targetSpawn, AudioClip clipToPlay, float volume)
    {
        if (clipToPlay == null) return;

        AudioSource audioSource = targetSpawn.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = targetSpawn.AddComponent<AudioSource>();
        }

        audioSource.clip = clipToPlay;
        audioSource.spatialBlend = 1.0f; // Maintains immersive 3D AR space positioning
        audioSource.volume = volume;
        audioSource.Play();
    }
}