using UnityEngine;

public class TitrationController : MonoBehaviour
{
    [Header("Target Renderer")]
    [Tooltip("Drag the 3D object with the liquid material here.")]
    public MeshRenderer liquidRenderer;

    [Header("Simulation Parameters")]
    [Range(0f, 1f)]
    public float titrationProgress = 0f;
    
    [Range(-1f, 1f)]
    public float fillAmount = 0.5f; // Adjust range based on your mesh's local Y scale

    // Cache the material and property IDs for performance
    private Material liquidMaterial;
    private int titrationProgressID;
    private int fillAmountID;

    void Start()
    {
        if (liquidRenderer != null)
        {
            // Accessing .material creates a clone for this specific object,
            // preventing accidental changes to your saved project asset.
            liquidMaterial = liquidRenderer.material;
        }
        else
        {
            Debug.LogError("Liquid Renderer is not assigned!");
        }

        // Caching property IDs is significantly faster than using string names every frame
        titrationProgressID = Shader.PropertyToID("_TitrationProgress");
        fillAmountID = Shader.PropertyToID("_FillAmount");
    }

    void Update()
    {
        // For testing in the Unity Editor: Updates the shader when you move the sliders in the Inspector
        if (liquidMaterial != null)
        {
            liquidMaterial.SetFloat(titrationProgressID, titrationProgress);
            liquidMaterial.SetFloat(fillAmountID, fillAmount);
        }
    }

    /// <summary>
    /// Call this method from your AR interaction system when a drop falls from the burette.
    /// </summary>
    /// <param name="dropVolume">How much the liquid level should rise.</param>
    /// <param name="reactionAmount">How much closer the liquid gets to the endpoint color.</param>
    public void AddDrop(float dropVolume, float reactionAmount)
    {
        fillAmount += dropVolume;
        titrationProgress += reactionAmount;
        
        // Clamp progress to ensure it doesn't exceed the 0 to 1 range
        titrationProgress = Mathf.Clamp01(titrationProgress);
        
        // Update the shader immediately
        if (liquidMaterial != null)
        {
            liquidMaterial.SetFloat(titrationProgressID, titrationProgress);
            liquidMaterial.SetFloat(fillAmountID, fillAmount);
        }
    }
}