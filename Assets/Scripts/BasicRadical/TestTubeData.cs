using UnityEngine;

public class TestTubeData : MonoBehaviour
{
    public int tubeIndex; // Assign 0-5 in inspector
    public bool isUsed = false;
    public bool hasPrecipitate = false;
    public GameObject liquidMesh; // Drag your Liquid_R1_Cn object here
    
    // Call this to update the visual appearance
    public void UpdateLiquidVisual(Color color, float fillAmount)
    {
        Renderer rend = liquidMesh.GetComponent<Renderer>();
        rend.material.SetColor("_LiquidColor", color);
        rend.material.SetFloat("_FillLevel", fillAmount);
    }
}