using UnityEngine;

public class PlanetData : MonoBehaviour
{
    [Tooltip("The page number on the dashboard (e.g., 0 for Sun, 1 for Mercury)")]
    public int dashboardPageIndex;

    [Tooltip("The high-quality 3D Prefab to spawn when this planet is double-tapped")]
    public GameObject inspectionPrefab;
}