using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
// 1. Import the New Input System Namespace
using UnityEngine.InputSystem; 

[RequireComponent(typeof(ARRaycastManager))]
public class PlacementWithDraggingDroppingController : MonoBehaviour
{
    [SerializeField]
    private GameObject placedPrefab;

    [SerializeField]
    private GameObject welcomePanel;

    [SerializeField]
    private Button dismissButton;

    [SerializeField]
    private Button lockButton;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private float defaultRotation = 0;

    private GameObject placedObject;
    private Vector2 touchPosition = default;
    private ARRaycastManager arRaycastManager;
    private bool isLocked = false;
    private bool onTouchHold = false;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake() 
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        dismissButton.onClick.AddListener(Dismiss);

        if(lockButton != null)
        {
            lockButton.onClick.AddListener(Lock);
        }
    }
    
    private void Dismiss() => welcomePanel.SetActive(false);

    private void Lock() 
    {
        isLocked = !isLocked;
        lockButton.GetComponentInChildren<Text>().text = isLocked ? "Locked" : "Unlocked";
    }

    void Update()
    {
        if(welcomePanel.activeSelf)
            return;

        bool inputDetected = false;
        bool inputBegan = false;
        bool inputEnded = false;

        // 2. Handle Inputs using New Input System API (Works for both Simulator Mouse & Phone Touch)
        if (Pointer.current != null)
        {
            touchPosition = Pointer.current.position.ReadValue();

            // Check if user just started clicking / tapping
            if (Pointer.current.press.wasPressedThisFrame)
            {
                inputDetected = true;
                inputBegan = true;
            }
            // Check if user is currently holding down the click / tap
            else if (Pointer.current.press.isPressed)
            {
                inputDetected = true;
            }
            // Check if user just lifted their mouse button / finger
            else if (Pointer.current.press.wasReleasedThisFrame)
            {
                inputEnded = true;
            }
        }

        // Logic to select the object via Physics Raycast
        if (inputBegan)
        {
            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            RaycastHit hitObject;
            if (Physics.Raycast(ray, out hitObject))
            {
                if (placedObject != null && hitObject.transform.gameObject == placedObject)
                {
                    onTouchHold = !isLocked;
                }
            }
        }

        if (inputEnded)
        {
            onTouchHold = false;
        }

        // Logic to move/place the object via AR Raycast
        if (inputDetected && arRaycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            if (placedObject == null)
            {
                if (defaultRotation > 0)
                {
                    placedObject = Instantiate(placedPrefab, hitPose.position, Quaternion.identity);
                    placedObject.transform.Rotate(Vector3.up, defaultRotation);
                }
                else 
                {
                    placedObject = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);
                }
            }
            else 
            {
                if (onTouchHold)
                {
                    placedObject.transform.position = hitPose.position;
                    if (defaultRotation == 0)
                    {
                        placedObject.transform.rotation = hitPose.rotation;
                    }
                }
            }
        }
    }
}