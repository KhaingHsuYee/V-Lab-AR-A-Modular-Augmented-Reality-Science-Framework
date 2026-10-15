using UnityEngine;
using DG.Tweening;

public class LabController : MonoBehaviour
{
    [Header("Lab Elements")]
    public GameObject metalLoop;
    public GameObject burnButton;

    [Header("Salt Prefabs")]
    public GameObject prefabNaCl;
    public GameObject prefabLiCl;
    public GameObject prefabCuCl2;
    public GameObject prefabKCl;

    private Transform activeBurnerTransform;
    private ParticleSystem activeFlameParticles;
    private GameObject currentSpawnedDish;
    private string selectedChemical;
    private Vector3 loopHomePosition;

    void Start()
    {
        loopHomePosition = metalLoop.transform.position;
        burnButton.SetActive(false);
    }

    // Called automatically when the burner is spawned via AR
    public void InitializeBurner(Transform burnerTransform, ParticleSystem flameSystem)
    {
        activeBurnerTransform = burnerTransform;
        activeFlameParticles = flameSystem;
    }

    // Linked to your UI Salt Buttons
    public void SelectSalt(string chemicalName)
    {
        if (activeBurnerTransform == null) return;
        if (currentSpawnedDish != null) Destroy(currentSpawnedDish);

        selectedChemical = chemicalName;

        // Choose the correct prefab
        GameObject targetPrefab = null;
        switch (chemicalName)
        {
            case "NaCl":  targetPrefab = prefabNaCl; break;
            case "LiCl":  targetPrefab = prefabLiCl; break;
            case "CuCl2": targetPrefab = prefabCuCl2; break;
            case "KCl":   targetPrefab = prefabKCl; break;
        }

        // Spawn dish 15 cm to the right of the burner
        Vector3 spawnPos = activeBurnerTransform.position + (activeBurnerTransform.right * 0.15f);
        currentSpawnedDish = Instantiate(targetPrefab, spawnPos, activeBurnerTransform.rotation);

        burnButton.SetActive(true);
    }

    // Linked to your UI "BURN" Button
    public void ExecuteFlameTest()
    {
        burnButton.SetActive(false);

        Vector3 dishPos = currentSpawnedDish.transform.position;
        // Position the loop directly over the flame center (roughly 20cm up from base)
        Vector3 flamePos = activeBurnerTransform.position + (Vector3.up * 0.2f); 

        Sequence loopSequence = DOTween.Sequence();

        // 1. Move over the dish and dip into the salt particles
        loopSequence.Append(metalLoop.transform.DOMove(dishPos + Vector3.up * 0.06f, 0.7f)); 
        loopSequence.Append(metalLoop.transform.DOMove(dishPos, 0.3f));                     
        loopSequence.Append(metalLoop.transform.DOMove(dishPos + Vector3.up * 0.06f, 0.3f)); 

        // 2. Move directly into the Bunsen flame
        loopSequence.Append(metalLoop.transform.DOMove(flamePos, 0.8f));

        // 3. Swap the color exactly as the loop registers inside the flame
        loopSequence.AppendCallback(() => ApplyFlameColor(selectedChemical));

        // 4. Leave it cooking for 2.5 seconds, then return home and clear colors
        loopSequence.AppendInterval(2.5f);
        loopSequence.Append(metalLoop.transform.DOMove(loopHomePosition, 1.0f));
        loopSequence.OnComplete(() => ResetFlameColor());
    }

    void ApplyFlameColor(string chemical)
    {
        if (activeFlameParticles == null) return;
        var main = activeFlameParticles.main;

        switch (chemical)
        {
            case "NaCl":  main.startColor = new Color(1.0f, 0.55f, 0.0f); break; // Orange-Yellow
            case "LiCl":  main.startColor = new Color(1.0f, 0.0f, 0.25f); break; // Carmine Red
            case "CuCl2": main.startColor = new Color(0.0f, 0.85f, 0.65f); break; // Teal / Turquoise
            case "KCl":   main.startColor = new Color(0.75f, 0.55f, 1.0f); break; // Light Lilac
        }
    }

    void ResetFlameColor()
    {
        if (activeFlameParticles == null) return;
        var main = activeFlameParticles.main;
        main.startColor = new Color(0.1f, 0.5f, 1.0f); // Reset back to natural gas blue
    }
}