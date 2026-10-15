using UnityEngine;

public class DynamicPrecipitate : MonoBehaviour
{
    public Material particleMaterial;

    public ParticleSystem CreatePrecipitateEffect(Transform targetTestTube, Color precipitateColor, float liquidHeight)
    {
        GameObject pgo = new GameObject("PrecipitateParticles");
        pgo.transform.SetParent(targetTestTube);
        pgo.transform.localPosition = new Vector3(0, liquidHeight * 0.5f, 0);
        
        ParticleSystem ps = pgo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        // Randomized lifetime for a swirling, natural look
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); 
        main.startSpeed = 0.02f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.03f);
        main.startColor = precipitateColor;
        main.gravityModifier = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 200f; // Increased for density[cite: 4]

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 0.06f;
        shape.length = liquidHeight;

        var noise = ps.noise;
        noise.enabled = true;
        noise.frequency = 1.5f;
        noise.strength = 0.08f;
        noise.scrollSpeed = 0.2f;

        ParticleSystemRenderer psRenderer = pgo.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = particleMaterial != null ? particleMaterial : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        psRenderer.sortingOrder = 1;

        ps.Play();
        return ps;
    }
}