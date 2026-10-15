using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UIElements; // Required for UIDocument
using DG.Tweening; 

public class Flame_ARPlacement : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject bunsenBurnerPrefab;
    
    // CHANGE: Point this to the GameObject in your hierarchy that has the UIDocument component
    public GameObject saltMenuUIDocument; 

    [Header("Audio Clips")]
    public AudioClip dropSoundClip; 

    private GameObject spawnedBurner;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private float lastClickTime;
    private const float doubleClickDelay = 0.3f;

    void Update()
    {
        if (Application.isEditor)
        {
            if (Input.GetMouseButtonDown(0))
                ProcessInput(Input.mousePosition);
        }
        else
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                    ProcessInput(touch.position);
            }
        }
    }

    void ProcessInput(Vector2 screenPosition)
    {
        float timeSinceLastClick = Time.time - lastClickTime;
        lastClickTime = Time.time;

        if (timeSinceLastClick <= doubleClickDelay)
            HandleDoubleTap(screenPosition);
        else if (spawnedBurner == null)
            HandleSingleTap(screenPosition);
    }

    void HandleSingleTap(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 targetLandingPosition = hitPose.position;
            Vector3 skySpawnPosition = targetLandingPosition + Vector3.up * 1.5f;

            spawnedBurner = Instantiate(bunsenBurnerPrefab, skySpawnPosition, hitPose.rotation);
            spawnedBurner.tag = "Burner"; // Ensure your prefab has the "Burner" tag!

            AudioSource dropSource = spawnedBurner.GetComponent<AudioSource>();
            if (dropSource != null && dropSoundClip != null)
            {
                dropSource.clip = dropSoundClip;
                dropSource.Play();
            }

            spawnedBurner.transform.DOMove(targetLandingPosition, 0.6f).SetEase(Ease.OutBounce);
        }
    }

    void HandleDoubleTap(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if we tapped the burner
            if (hit.collider.CompareTag("Burner"))
            {
                // Toggle the new UI Document
                if (saltMenuUIDocument != null)
                {
                    bool isMenuActive = saltMenuUIDocument.activeSelf;
                    saltMenuUIDocument.SetActive(!isMenuActive);
                }
            }
        }
    }
}