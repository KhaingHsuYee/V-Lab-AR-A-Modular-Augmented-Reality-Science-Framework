using UnityEngine;
using System.Collections.Generic;

public class AtomGenerator : MonoBehaviour
{
    public ElementData_Periodic currentElement;
    public GameObject protonPrefab, neutronPrefab, electronPrefab;
    
    private List<Transform> orbitRings = new List<Transform>();
    private Transform nucleusContainer; // NEW: Container to hold and animate the core

    public void Initialize(ElementData_Periodic elementData)
    {
        currentElement = elementData;
        
        foreach (Transform child in transform) { Destroy(child.gameObject); }
        orbitRings.Clear();

        GenerateAtom();
    }

    void GenerateAtom()
    {
        if (currentElement == null) return;

        // 1. Increased Base Scale Math
        int totalNucleons = currentElement.protons + currentElement.neutrons;
        float nucleusSpread = 0.02f + (totalNucleons * 0.0004f); 
        
        float baseRadius = 0.35f;     // Widened to push rings toward the table edges
        float radiusStep = 0.12f;     // Increased gap between electron shells
        float particleScale = 0.06f;  // Enlarged the nucleus particles
        float electronScale = 0.045f; // Enlarged the electrons

        GameObject nucObj = new GameObject("Nucleus");
        nucObj.transform.SetParent(transform, false); 
        nucObj.transform.localPosition = Vector3.zero;
        nucleusContainer = nucObj.transform;

        for (int i = 0; i < currentElement.protons; i++)
        {
            GameObject p = Instantiate(protonPrefab, nucleusContainer);
            p.transform.localPosition = Random.insideUnitSphere * nucleusSpread;
            p.transform.localScale = Vector3.one * particleScale; 
        }
            
        for (int i = 0; i < currentElement.neutrons; i++)
        {
            GameObject n = Instantiate(neutronPrefab, nucleusContainer);
            n.transform.localPosition = Random.insideUnitSphere * nucleusSpread;
            n.transform.localScale = Vector3.one * particleScale;
        }

        float currentRadius = baseRadius;
        for (int shell = 0; shell < currentElement.electronShells.Length; shell++)
        {
            GameObject orbitRing = new GameObject($"Orbit_Shell_{shell}");
            orbitRing.transform.SetParent(transform, false); 
            orbitRing.transform.localPosition = Vector3.zero;
            orbitRings.Add(orbitRing.transform);

            // 2. Visually Beautiful Ring Colors
            LineRenderer lr = orbitRing.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.004f; // Slightly thicker rings
            lr.endWidth = 0.004f;
            lr.positionCount = 50;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            
            // Smoothly transition the alpha from 0.5 (inner ring) to 0.1 (outermost ring)
            float ringAlpha = Mathf.Lerp(0.5f, 0.1f, (float)shell / currentElement.electronShells.Length);

            // Apply the calculated alpha to your HDR color
            Color dynamicCyan = new Color(0.0f, 2.4f, 3.0f, ringAlpha); 
            lr.startColor = dynamicCyan;
            lr.endColor = dynamicCyan;
            for (int i = 0; i < 50; i++)
            {
                float rad = (i / 50f) * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * currentRadius, 0, Mathf.Sin(rad) * currentRadius));
            }

            int electronCount = currentElement.electronShells[shell];
            for (int e = 0; e < electronCount; e++)
            {
                float angle = e * Mathf.PI * 2f / electronCount;
                Vector3 localPos = new Vector3(Mathf.Cos(angle) * currentRadius, 0, Mathf.Sin(angle) * currentRadius);
                
                GameObject el = Instantiate(electronPrefab, orbitRing.transform);
                el.transform.localPosition = localPos;
                el.transform.localScale = Vector3.one * electronScale;
            }
            currentRadius += radiusStep; 
        }
    }

    void Update()
    {
        // Animate the flat electron rings
        for (int i = 0; i < orbitRings.Count; i++)
        {
            float speed = 150f / (i + 1); 
            orbitRings[i].Rotate(Vector3.up, speed * Time.deltaTime);
        }

        // --- NEW: Animate the Nucleus ---
        if (nucleusContainer != null)
        {
            // 1. Tumbling rotation on all 3 axes
            nucleusContainer.Rotate(new Vector3(15f, 25f, 10f) * Time.deltaTime);

            // 2. Subtle breathing/pulsing effect using Sine wave
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.04f;
            nucleusContainer.localScale = new Vector3(pulse, pulse, pulse);
        }
    }
}