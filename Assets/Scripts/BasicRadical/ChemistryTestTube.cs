using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // We use DOTween to make the cloudiness fade in smoothly!

public class ChemistryTestTube : MonoBehaviour
{
    [Header("References")]
    public Liquid1 liquidPhysicsScript; // Drag your Liquid script here
    public Renderer liquidRenderer;    // Drag the Liquid Mesh here to access the material

    [Header("Simulation Settings")]
    [Tooltip("How much the liquid rises per drop (0.05 = 5%)")]
    public float dropVolume = 0.05f; 
    [Tooltip("How fast it turns cloudy per drop")]
    public float cloudinessPerDrop = 0.15f; 

    private float currentCloudiness = 0f;

    // The list of chemicals from your UI
    public enum ReagentType { HCl, KI, NaOH, K2CrO4, H2S, K4FeCN6, K3FeCN6, Buffer, NH42S }

    // This struct holds the rules for what happens when a chemical is added
    [System.Serializable]
    public struct ReactionData
    {
        public bool formsPrecipitate;
        public Color precipitateColor;
    }

    // Our Master Chemistry Recipe Book
    private Dictionary<ReagentType, ReactionData> reactions;

    void Start()
    {
        if (liquidRenderer == null && liquidPhysicsScript != null)
        {
            liquidRenderer = liquidPhysicsScript.GetComponent<Renderer>();
        }

        // Initialize the shader to be perfectly clear when the game starts
        if (liquidRenderer != null)
        {
            liquidRenderer.material.SetFloat("_Cloudiness", 0f);
        }

        // --- DEFINE THE CHEMISTRY RULES HERE ---
        reactions = new Dictionary<ReagentType, ReactionData>
        {
            // 1. Dilute HCl -> White Precipitate
            { ReagentType.HCl, new ReactionData { formsPrecipitate = true, precipitateColor = Color.white } },
            
            // 2. Potassium Iodide (KI) -> Bright Yellow Precipitate
            { ReagentType.KI, new ReactionData { formsPrecipitate = true, precipitateColor = Color.yellow } },
            
            // 3. Hydrogen Sulphide (H2S) -> Black Precipitate
            { ReagentType.H2S, new ReactionData { formsPrecipitate = true, precipitateColor = Color.black } },

            // 4. Buffer Solution -> NO Precipitate (Just dilutes the liquid)
            { ReagentType.Buffer, new ReactionData { formsPrecipitate = false, precipitateColor = Color.clear } },

            // You can add the rest of your 9 reagents here!
        };
    }

    void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        // This will automatically test with HCl when you press Space
        AddDrop(ReagentType.HCl); 
    }
}

    // This function will be called by your UI "ADD" button
    public void AddDrop(ReagentType reagentAdded)
    {
        // 1. Stop if the test tube is full (1.0 = 100%)
        if (liquidPhysicsScript.fillPercentage >= 1.0f)
        {
            Debug.Log("Test tube is full! Stop dripping.");
            // You can call your UI Manager here to show a pop-up!
            return;
        }

        // 2. Increase the liquid volume physically
        liquidPhysicsScript.fillPercentage += dropVolume;
        
        // Clamp it so it never goes above 1.0
        liquidPhysicsScript.fillPercentage = Mathf.Clamp01(liquidPhysicsScript.fillPercentage);

        // 3. Apply the Chemistry Rules!
        if (reactions.ContainsKey(reagentAdded))
        {
            ReactionData currentReaction = reactions[reagentAdded];

            if (currentReaction.formsPrecipitate)
            {
                // A. Change the shader's target color to match the chemical (White, Yellow, etc.)
                liquidRenderer.material.SetColor("_PrecipitateColor", currentReaction.precipitateColor);

                // B. Increase the cloudiness variable
                currentCloudiness += cloudinessPerDrop;
                currentCloudiness = Mathf.Clamp01(currentCloudiness); // Max is 1.0 (Solid)

                // C. Smoothly fade the material from clear to cloudy over 0.5 seconds using DOTween!
                liquidRenderer.material.DOFloat(currentCloudiness, "_Cloudiness", 0.5f);
            }
        }
    }
}