using UnityEngine;
using UnityEngine.InputSystem;

public class SolarSystemInteractionManager : MonoBehaviour
{
    [Header("UI & Spawning Setup")]
    public GameObject dashboardPrefab;

    private float lastTapTime = 0f;
    private readonly float doubleTapThreshold = 0.3f; 
    
    // State Trackers
    private GameObject currentActiveClone;
    private GameObject currentActiveDashboard;
    private GameObject hiddenSolarSystem;

    private void Update()
    {
        bool wasPressed = false;
        Vector2 inputPosition = Vector2.zero;

        // Read Input
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            wasPressed = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            wasPressed = true;
            inputPosition = Mouse.current.position.ReadValue();
        }

        // Timer Logic
        if (wasPressed)
        {
            if (Time.time - lastTapTime < doubleTapThreshold)
            {
                HandleDoubleTap(inputPosition);
            }
            lastTapTime = Time.time;
        }
    }

    private void HandleDoubleTap(Vector2 screenPosition)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // STATE 1: We are looking at the main Solar System
            if (currentActiveClone == null)
            {
                PlanetData tappedPlanet = hit.collider.GetComponent<PlanetData>();
                if (tappedPlanet != null)
                {
                    // Find the root object of the solar system and hide it
                    hiddenSolarSystem = tappedPlanet.transform.root.gameObject;
                    hiddenSolarSystem.SetActive(false);
                    
                    // Spawn the clone exactly where the solar system was sitting
                    OpenInspectionView(tappedPlanet, hiddenSolarSystem.transform.position);
                }
            }
            // STATE 2: We are looking at the single Inspection Clone
            else 
            {
                // Check if the object we just double-tapped is our active clone
                if (hit.transform.IsChildOf(currentActiveClone.transform) || hit.transform.gameObject == currentActiveClone)
                {
                    CloseInspectionView();
                }
            }
        }
    }

    private void OpenInspectionView(PlanetData planetData, Vector3 spawnPosition)
    {
        if (planetData.inspectionPrefab == null) return;

        // 1. Spawn the high-quality planet clone
        currentActiveClone = Instantiate(planetData.inspectionPrefab, spawnPosition, Quaternion.identity);

        // 2. Spawn the Dashboard close to the planet
        if (dashboardPrefab != null)
        {
            // Use a fixed world offset (-0.25 on the X axis) to place it tightly to the left.
            // You can change 0.25f to a smaller number like 0.15f if you want it even closer!
            Vector3 dashboardPos = spawnPosition + new Vector3(-0.25f, 0, 0); 
            
            currentActiveDashboard = Instantiate(dashboardPrefab, dashboardPos, Quaternion.identity);

            // Make the UI face the camera exactly ONCE when it spawns, then it locks in place.
            currentActiveDashboard.transform.LookAt(Camera.main.transform);
            currentActiveDashboard.transform.Rotate(0, 180, 0);

            // Send the planet index to the dashboard to update text and audio
            SolarSystemDashboard dashboardScript = currentActiveDashboard.GetComponent<SolarSystemDashboard>();
            if (dashboardScript != null)
            {
                dashboardScript.LoadPlanetData(planetData.dashboardPageIndex);
            }
        }
    }

    private void CloseInspectionView()
    {
        // 1. Destroy the clone and dashboard
        if (currentActiveClone != null) Destroy(currentActiveClone);
        if (currentActiveDashboard != null) Destroy(currentActiveDashboard);

        // 2. Bring the main solar system back!
        if (hiddenSolarSystem != null)
        {
            hiddenSolarSystem.SetActive(true);
        }
    }
}