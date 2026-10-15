using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
[ExecuteInEditMode]
public class Liquid1 : MonoBehaviour
{
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;
 
    [Header("Chemistry Controls")]
    [Range(0f, 1f)]
    [Tooltip("0 is completely Empty, 1 is completely Full.")]
    public float fillPercentage = 0.5f;

    [Header("Test Tube Calibration")]
    [Tooltip("The raw value that makes the tube look completely EMPTY. Usually a positive number like 0.5 or 1.0.")]
    public float emptyCalibrationValue = 0.5f;
    [Tooltip("The raw value that makes the tube look completely FULL. Usually a negative number like -0.5 or -1.0.")]
    public float fullCalibrationValue = -0.5f;

    [Header("Physics Settings")]
    [SerializeField] float MaxWobble = 0.03f;
    [SerializeField] float WobbleSpeedMove = 1f;
    [SerializeField] float Recovery = 1f;
    [SerializeField] float Thickness = 1f;
    [Range(0, 1)] public float CompensateShapeAmount;
    
    [Header("References")]
    [SerializeField] Mesh mesh;
    [SerializeField] Renderer rend;
    
    Vector3 pos;
    Vector3 lastPos;
    Vector3 velocity;
    Quaternion lastRot;
    Vector3 angularVelocity;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float sinewave;
    float time = 0.5f;
    Vector3 comp;
 
    void Start()
    {
        GetMeshAndRend();
    }
 
    private void OnValidate()
    {
        GetMeshAndRend();
    }
 
    void GetMeshAndRend()
    {
        if (mesh == null) mesh = GetComponent<MeshFilter>().sharedMesh;
        if (rend == null) rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float deltaTime = 0;
        switch (updateMode)
        {
            case UpdateMode.Normal: deltaTime = Time.deltaTime; break;
            case UpdateMode.UnscaledTime: deltaTime = Time.unscaledDeltaTime; break;
        }
 
        time += deltaTime;
 
        if (deltaTime != 0)
        {
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, (deltaTime * Recovery));
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, (deltaTime * Recovery));
 
            pulse = 2 * Mathf.PI * WobbleSpeedMove;
            sinewave = Mathf.Lerp(sinewave, Mathf.Sin(pulse * time), deltaTime * Mathf.Clamp(velocity.magnitude + angularVelocity.magnitude, Thickness, 10));
 
            wobbleAmountX = wobbleAmountToAddX * sinewave;
            wobbleAmountZ = wobbleAmountToAddZ * sinewave;
 
            velocity = (lastPos - transform.position) / deltaTime;
            angularVelocity = GetAngularVelocity(lastRot, transform.rotation);
 
            wobbleAmountToAddX += Mathf.Clamp((velocity.x + (velocity.y * 0.2f) + angularVelocity.z + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (velocity.y * 0.2f) + angularVelocity.x + angularVelocity.y) * MaxWobble, -MaxWobble, MaxWobble);
        }
 
        rend.sharedMaterial.SetFloat("_WobbleX", wobbleAmountX);
        rend.sharedMaterial.SetFloat("_WobbleZ", wobbleAmountZ);
 
        UpdatePos(deltaTime);
 
        lastPos = transform.position;
        lastRot = transform.rotation;
    }
 
    void UpdatePos(float deltaTime)
    {
        // THE FIX: We map your 0-1 percentage to the raw offset values the shader needs!
        float mappedFillAmount = Mathf.Lerp(emptyCalibrationValue, fullCalibrationValue, fillPercentage);

        Vector3 worldPos = transform.TransformPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.center.y, mesh.bounds.center.z));
        if (CompensateShapeAmount > 0)
        {
            if (deltaTime != 0) comp = Vector3.Lerp(comp, (worldPos - new Vector3(0, GetLowestPoint(), 0)), deltaTime * 10);
            else comp = (worldPos - new Vector3(0, GetLowestPoint(), 0));
 
            // We use mappedFillAmount here instead of the raw fillAmount
            pos = worldPos - transform.position - new Vector3(0, mappedFillAmount - (comp.y * CompensateShapeAmount), 0);
        }
        else
        {
            pos = worldPos - transform.position - new Vector3(0, mappedFillAmount, 0);
        }

        rend.sharedMaterial.SetVector("_FillAmount", pos);
    }
 
    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        if (Mathf.Abs(q.w) > 1023.5f / 1024.0f) return Vector3.zero;
        float gain;
        if (q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle) * Time.deltaTime);
        }
        Vector3 angularVelocity = new Vector3(q.x * gain, q.y * gain, q.z * gain);
        if (float.IsNaN(angularVelocity.z)) angularVelocity = Vector3.zero;
        return angularVelocity;
    }
 
    float GetLowestPoint()
    {
        float lowestY = float.MaxValue;
        Vector3 lowestVert = Vector3.zero;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 position = transform.TransformPoint(vertices[i]);
            if (position.y < lowestY)
            {
                lowestY = position.y;
                lowestVert = position;
            }
        }
        return lowestVert.y;
    }
}