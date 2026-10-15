using UnityEngine;

public class FluidWobbleController : MonoBehaviour
{
    [Header("Level Settings")]
    [Range(0f, 1f)] public float fillLevel = 0.5f;

    [Header("Wobble Settings")]
    public float maxWobble = 0.03f;    
    public float wobbleSpeed = 5.0f;   
    public float recoveryRate = 2.5f;  

    private Renderer rend;
    private Material matInstance;
    private Vector3 lastPos;
    private Vector3 lastRot;
    
    private float wobbleAmountToAddX;
    private float wobbleAmountToAddZ;
    private float time;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            matInstance = rend.material; 
        }
        else
        {
            Debug.LogError("FluidWobbleController needs a Renderer component alongside it!");
        }
        
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    void Update()
    {
        if (matInstance == null || rend == null) return;

        time += Time.deltaTime;

        // 1. DYNAMICALLY CALCULATE WORLD SPACE COORDINATES
        // Get the bounding box of the mesh in world space coordinates
        Bounds worldBounds = rend.bounds;
        
        float totalWorldHeight = worldBounds.size.y; 
        float worldBottomY = worldBounds.min.y;      

        // 2. SEND LIVE WORLD COORDINATES TO SHADER
        matInstance.SetFloat("_FillLevel", fillLevel);
        matInstance.SetFloat("_BeakerHeight", totalWorldHeight);
        
        // Calculate the bottom relative to the object transform origin (which the shader uses)
        float bottomOffset = worldBottomY - transform.position.y;
        matInstance.SetFloat("_BottomOffset", bottomOffset);

        // 3. WOBBLE PHYSICS DECAY CYCLES
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * recoveryRate);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * recoveryRate);

        float pulse = 2 * Mathf.PI * wobbleSpeed;
        float wobbleX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        float wobbleZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        matInstance.SetFloat("_WobbleX", wobbleX);
        matInstance.SetFloat("_WobbleZ", wobbleZ);

        Vector3 velocity = (lastPos - transform.position) / Time.deltaTime;
        Vector3 angularVelocity = transform.rotation.eulerAngles - lastRot;

        angularVelocity.x = MapAngle(angularVelocity.x);
        angularVelocity.z = MapAngle(angularVelocity.z);

        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.1f)) * maxWobble, -maxWobble, maxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.1f)) * maxWobble, -maxWobble, maxWobble);

        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    private float MapAngle(float angle)
    {
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        return angle;
    }
}