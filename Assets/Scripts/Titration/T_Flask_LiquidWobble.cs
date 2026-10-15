using UnityEngine;

[ExecuteAlways]
public class T_Flask_LiquidWobble : MonoBehaviour
{
    [Header("Setup")]
    public MeshRenderer liquidRenderer;
    private Material liquidMaterial;

    [Header("Linear Wobble (Moving the flask)")]
    public float maxWobble = 0.05f; 
    public float wobbleSpeed = 15f; 
    public float recoverySpeed = 3f; 
    public float movementSensitivity = 0.5f; 

    [Header("Titration Swirl (Shaking the flask)")]
    [Range(0f, 0.1f)]
    [Tooltip("Turn this up to simulate the flask being swirled in a circle.")]
    public float swirlIntensity = 0.0f;
    public float swirlSpeed = 20f; // How fast the vortex spins

    private Vector3 lastPos;
    private Vector3 velocity;
    private float wobbleAmountX;
    private float wobbleAmountZ;
    private float wobbleToAddX;
    private float wobbleToAddZ;
    private float time;

    private int wobbleX_ID;
    private int wobbleZ_ID;

    void Start()
    {
        if (liquidRenderer != null)
        {
            // If the game is actually running on the phone or in Play Mode, create a safe clone
            if (Application.isPlaying)
            {
                liquidMaterial = liquidRenderer.material;
            }
            // If we are just testing in the Unity Editor, use the shared asset to prevent memory leaks
            else
            {
                liquidMaterial = liquidRenderer.sharedMaterial;
            }
        }
        
        wobbleX_ID = Shader.PropertyToID("_WobbleX");
        wobbleZ_ID = Shader.PropertyToID("_WobbleZ");
    }
    void Update()
    {
        if (liquidMaterial == null) return;

        float dt = Time.deltaTime;
        if (dt == 0f) dt = 0.016f; 

        time += dt;

        // --- 1. LINEAR PHYSICS (Moving back and forth) ---
        velocity = (lastPos - transform.position) / dt;
        
        wobbleToAddX = Mathf.Clamp(velocity.x * movementSensitivity, -maxWobble, maxWobble);
        wobbleToAddZ = Mathf.Clamp(velocity.z * movementSensitivity, -maxWobble, maxWobble);

        wobbleAmountX = Mathf.Lerp(wobbleAmountX, 0, dt * recoverySpeed);
        wobbleAmountZ = Mathf.Lerp(wobbleAmountZ, 0, dt * recoverySpeed);

        wobbleAmountX += wobbleToAddX;
        wobbleAmountZ += wobbleToAddZ;

        float linearSloshX = wobbleAmountX * Mathf.Sin(wobbleSpeed * time);
        float linearSloshZ = wobbleAmountZ * Mathf.Sin(wobbleSpeed * time);

        // --- 2. SWIRL PHYSICS (Titration mixing) ---
        // Sine on X and Cosine on Z creates a perfect circle
        float swirlX = swirlIntensity * Mathf.Sin(swirlSpeed * time);
        float swirlZ = swirlIntensity * Mathf.Cos(swirlSpeed * time);

        // --- 3. COMBINE AND SEND TO SHADER ---
        liquidMaterial.SetFloat(wobbleX_ID, linearSloshX + swirlX);
        liquidMaterial.SetFloat(wobbleZ_ID, linearSloshZ + swirlZ);

        lastPos = transform.position;
    }
}