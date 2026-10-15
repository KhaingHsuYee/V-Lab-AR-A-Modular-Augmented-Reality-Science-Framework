using UnityEngine;

[ExecuteAlways]
public class LiquidWobble_Fill_Cont : MonoBehaviour
{
    private Renderer rend;
    private Vector3 lastPos;
    private Vector2 wobble;
    private Vector2 wobbleVelocity;

    [Header("Slosh Physics")]
    public float tension = 8f; 
    public float dampening = 0.08f; 
    public float movementScale = 2.5f; 

    void Start()
    {
        CacheRenderer();
    }

    void CacheRenderer()
    {
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
        if (rend != null)
        {
            lastPos = transform.position;
        }
    }

    void Update()
    {
        // 1. Safety check: ensure renderer exists
        if (rend == null)
        {
            CacheRenderer();
            if (rend == null) return; // Exit safely if still missing
        }

        // 2. Safety check: ensure material exists and has our properties
        Material mat = rend.sharedMaterial;
        
        // THIS IS THE CRITICAL LINE: It prevents the NullReferenceException
        if (mat == null || !mat.HasProperty("_WobbleX"))
        {
            return; 
        }

        // 3. Calculate physics and movement
        Vector3 movement = transform.position - lastPos;
        
        wobbleVelocity.x -= movement.x * movementScale;
        wobbleVelocity.y -= movement.z * movementScale; 

        wobbleVelocity -= wobble * tension * Time.deltaTime;
        wobbleVelocity *= (1f - dampening);
        wobble += wobbleVelocity * Time.deltaTime;

        // 4. Send to shader safely
        mat.SetFloat("_WobbleX", wobble.x);
        mat.SetFloat("_WobbleZ", wobble.y);

        lastPos = transform.position;
    }
}