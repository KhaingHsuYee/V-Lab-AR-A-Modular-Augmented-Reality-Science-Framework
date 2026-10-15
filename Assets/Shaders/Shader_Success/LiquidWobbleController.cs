using UnityEngine;

public class LiquidWobbleController : MonoBehaviour
{
    [Header("Wobble Settings")]
    public float maxWobble = 0.15f;    // Maximum wobble amplitude
    public float wobbleSpeed = 4.0f;   // How fast it sloshes back and forth
    public float recoveryRate = 1.5f;  // How quickly the liquid returns to rest

    private Renderer rend;
    private Vector3 lastPos;
    private Vector3 lastRot;
    
    private float wobbleAmountToAddX;
    private float wobbleAmountToAddZ;
    private float pulse;
    private float time;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("LiquidWobble script requires a Renderer component on the same GameObject!");
        }
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    void Update()
    {
        if (rend == null) return;

        time += Time.deltaTime;

        // 1. Decelerate/decay the wobble over time
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * recoveryRate);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * recoveryRate);

        // 2. Generate the sine wave oscillation
        pulse = 2 * Mathf.PI * wobbleSpeed;
        float wobbleX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        float wobbleZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        // 3. Push values to your custom shader properties
        rend.material.SetFloat("_WobbleX", wobbleX);
        rend.material.SetFloat("_WobbleZ", wobbleZ);

        // 4. Calculate physics forces based on movement and rotation deltas
        Vector3 velocity = (lastPos - transform.position) / Time.deltaTime;
        Vector3 angularVelocity = transform.rotation.eulerAngles - lastRot;

        // Account for euler angle wrap-around jumping (e.g. 359 -> 0)
        angularVelocity.x = MapAngle(angularVelocity.x);
        angularVelocity.z = MapAngle(angularVelocity.z);

        // 5. Add velocity forces to the wobble accumulator
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.1f)) * maxWobble, -maxWobble, maxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.1f)) * maxWobble, -maxWobble, maxWobble);

        // Keep tracking state
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