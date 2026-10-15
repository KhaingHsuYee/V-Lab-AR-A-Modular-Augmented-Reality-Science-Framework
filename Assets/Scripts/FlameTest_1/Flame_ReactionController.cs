using UnityEngine;
using DG.Tweening;
// Removed UnityEngine.VFX

public class Flame_ReactionController : MonoBehaviour
{
    [Header("Managers")]
    public Flame_ExperimentManager experimentManager;

    [Header("Dashboard Integration")]
    public ARDataController dashboardController; 
    public GameObject dashboardGameObject; 
    
    [Header("Element Data (Scriptable Objects)")]
    public ElementData dataNaCl;
    public ElementData dataLiCl;
    public ElementData dataCuCl2;
    public ElementData dataKCl;

    [Header("Prefabs")]
    public GameObject prefabNaCl;
    public GameObject prefabLiCl;
    public GameObject prefabCuCl2;
    public GameObject prefabKCl;
    public GameObject prefabBeaker;

    [Header("Audio")]
    public AudioClip dishDropSoundClip;
    public AudioClip saltScoopSFX;        
    public AudioClip chemicalSizzleSFX;
    public AudioClip loopDipSFX; 

    // Removed VFX Color Property Name

    [HideInInspector] public bool isAnimating = false;

    private string selectedSaltId;
    private bool isUnknown;
    private GameObject currentSpawnedDish;
    private GameObject currentSpawnedBeaker;
    private GameObject liveLoopObject;    
    private GameObject saltSampleOnLoop;  

    private Vector3 loopHomePosition;    
    private Quaternion loopHomeRotation;  
    
    public void SelectSalt(string btnId, bool unknownFlag)
    {
        GameObject activeBurner = GameObject.FindWithTag("Burner");
        if (activeBurner == null) return;

        ResetEquipment();
        selectedSaltId = btnId;
        isUnknown = unknownFlag;

        if (dashboardGameObject != null && !dashboardGameObject.activeSelf) 
        {
            dashboardGameObject.SetActive(true);
        }
        if (dashboardController != null) 
        {
            dashboardController.ShowInstructions();
        }

        Vector3 cameraToBurner = activeBurner.transform.position - Camera.main.transform.position;
        cameraToBurner.y = 0;
        cameraToBurner.Normalize();
        Vector3 viewRelativeRight = Quaternion.Euler(0, 90, 0) * cameraToBurner;

        Vector3 dishTargetPos = activeBurner.transform.position + (viewRelativeRight * 0.15f) - (cameraToBurner * 0.03f);
        Vector3 beakerTargetPos = activeBurner.transform.position - (viewRelativeRight * 0.15f) - (cameraToBurner * 0.03f);

        GameObject targetPrefab = GetPrefabByName(btnId);
        currentSpawnedDish = Instantiate(targetPrefab, dishTargetPos + Vector3.up * 1.0f, activeBurner.transform.rotation);
        currentSpawnedDish.transform.DOMove(dishTargetPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() => PlayAudio(currentSpawnedDish, dishDropSoundClip));

        Transform oldCanvas = currentSpawnedDish.transform.Find("DishUICanvas");
        if (oldCanvas != null) Destroy(oldCanvas.gameObject);

        currentSpawnedBeaker = Instantiate(prefabBeaker, beakerTargetPos + Vector3.up * 1.0f, activeBurner.transform.rotation);
        currentSpawnedBeaker.transform.DOMove(beakerTargetPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() => PlayAudio(currentSpawnedBeaker, dishDropSoundClip));
        
        liveLoopObject = currentSpawnedBeaker.transform.Find("MetalLoop").gameObject; 
        loopHomePosition = beakerTargetPos;
        loopHomeRotation = liveLoopObject.transform.rotation;
        
        Transform childTransform = liveLoopObject.transform.Find("SaltSampleOnLoop");
        if (childTransform != null)
        {
            saltSampleOnLoop = childTransform.gameObject;
            saltSampleOnLoop.SetActive(false);
        }
    }

    public void ExecutePickSample()
    {
        if (liveLoopObject == null || currentSpawnedDish == null) return;
        isAnimating = true;
        experimentManager.HideActionButton();
        
        GameObject activeBurner = GameObject.FindWithTag("Burner");
        Vector3 dishPosition = currentSpawnedDish.transform.position;
        Vector3 workingPosition = (dishPosition + activeBurner.transform.position) / 2f + (Vector3.up * 0.15f);

        ParticleSystem dishSaltParticles = currentSpawnedDish.GetComponentInChildren<ParticleSystem>();
        
        Sequence pickSequence = DOTween.Sequence();
        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition + Vector3.up * 0.06f, 0.6f));
        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition, 0.3f));
        
        pickSequence.AppendCallback(() =>
        {
            if (saltSampleOnLoop != null)
            {
                saltSampleOnLoop.SetActive(true);
                MatchSampleColor(saltSampleOnLoop, selectedSaltId);
            }
            if (dishSaltParticles != null) dishSaltParticles.Play();
            PlayAudio(liveLoopObject, saltScoopSFX);
            
            string displayName = isUnknown ? "Unknown Salt" : selectedSaltId.Replace("Btn_", "");
            experimentManager.UpdateStatusText("Loop Prepared", $"Sample loaded with {displayName}");
        });

        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition + Vector3.up * 0.06f, 0.3f));
        pickSequence.Append(liveLoopObject.transform.DOMove(workingPosition, 0.6f));

        pickSequence.OnComplete(() =>
        {
            isAnimating = false;
            experimentManager.SetButtonStateBurn(); 
        });
    }

    public void ExecuteBurnSample()
    {
        if (liveLoopObject == null) return;
        isAnimating = true;

        GameObject activeBurner = GameObject.FindWithTag("Burner");
        
        Transform flameAnchorTransform = activeBurner.transform.Find("FlameAnchor");
        Vector3 flamePosition = flameAnchorTransform != null ? flameAnchorTransform.position : activeBurner.transform.position + (Vector3.up * 0.22f);

        experimentManager.UpdateStatusText("Burning...", "Observe the flame color.");

        Sequence burnSequence = DOTween.Sequence();
        burnSequence.Append(liveLoopObject.transform.DOMove(flamePosition, 0.6f));
        burnSequence.Join(liveLoopObject.transform.DORotate(new Vector3(90f, liveLoopObject.transform.eulerAngles.y, liveLoopObject.transform.eulerAngles.z), 0.6f));

        burnSequence.AppendCallback(() =>
        {
            // Inject Particle System Color Here
            ApplyParticleFlameColor(activeBurner, selectedSaltId);
            PlayAudio(activeBurner, chemicalSizzleSFX);
            if (saltSampleOnLoop != null) saltSampleOnLoop.SetActive(false);
            
            if (dashboardGameObject != null && dashboardController != null)
            {
                ElementData currentData = GetElementData(selectedSaltId);
                if (currentData != null && !isUnknown) 
                {
                    dashboardController.Populate(currentData);
                    
                    dashboardGameObject.transform.position = activeBurner.transform.position + (Vector3.up * 0.45f);
                    if (Camera.main != null)
                    {
                        Vector3 targetPosition = new Vector3(Camera.main.transform.position.x, dashboardGameObject.transform.position.y, Camera.main.transform.position.z);
                        dashboardGameObject.transform.LookAt(targetPosition);
                        dashboardGameObject.transform.Rotate(0, 180, 0); 
                    }
                    dashboardGameObject.SetActive(true);
                }
            }
            
            if (isUnknown)
            {
                experimentManager.UpdateStatusText("Result", "Observing unknown flame...");
            }
            else
            {
                string colorResult = GetFlameColorName(selectedSaltId);
                experimentManager.UpdateStatusText("Result", $"Flame color is {colorResult}");
                experimentManager.MarkSaltTested(selectedSaltId); 
            }
        });

        burnSequence.AppendInterval(3.0f);
        
        burnSequence.Append(liveLoopObject.transform.DOMove(loopHomePosition, 0.8f));
        burnSequence.Join(liveLoopObject.transform.DORotateQuaternion(loopHomeRotation, 0.8f));
        burnSequence.AppendCallback(() => PlayAudio(liveLoopObject, loopDipSFX));

        burnSequence.OnComplete(() =>
        {
            ResetParticleFlameColor(activeBurner);
            isAnimating = false;
            
            if (isUnknown)
            {
                experimentManager.UpdateStatusText("Challenge Time!", "Identify the unknown element.");
                experimentManager.OpenUnknownSaltQuiz(selectedSaltId);
            }
            else
            {
                experimentManager.UpdateStatusText("Test Complete", "Cleaned loop in acid. Ready for next sample.");
                experimentManager.SetButtonStatePick(); 
            }
        });
    }

    private void ResetEquipment()
    {
        if (currentSpawnedDish != null) Destroy(currentSpawnedDish);
        if (currentSpawnedBeaker != null) Destroy(currentSpawnedBeaker);
        
        GameObject activeBurner = GameObject.FindWithTag("Burner");
        if (activeBurner != null) ResetParticleFlameColor(activeBurner);
    }

    private ElementData GetElementData(string btnId)
    {
        switch (btnId)
        {
            case "Btn_NaCl": return dataNaCl;
            case "Btn_LiCl": return dataLiCl;
            case "Btn_CuCl2": return dataCuCl2;
            case "Btn_KCl": return dataKCl;
            default: return null;
        }
    }

    private GameObject GetPrefabByName(string btnId)
    {
        switch (btnId)
        {
            case "Btn_NaCl": return prefabNaCl;
            case "Btn_LiCl": return prefabLiCl;
            case "Btn_CuCl2": return prefabCuCl2;
            case "Btn_KCl": return prefabKCl;
            default: return prefabNaCl;
        }
    }

    private void MatchSampleColor(GameObject sampleObject, string btnId)
    {
        Renderer rend = sampleObject.GetComponent<Renderer>();
        if (rend == null) return;

        Color targetColor = Color.white;
        if (btnId == "Btn_CuCl2") targetColor = new Color(0.15f, 0.65f, 0.55f); 
        rend.sharedMaterial.color = targetColor;
    }

    private string GetFlameColorName(string btnId)
    {
        switch (btnId)
        {
            case "Btn_NaCl": return "Golden Yellow (Sodium)";
            case "Btn_LiCl": return "Crimson Red (Lithium)";
            case "Btn_CuCl2": return "Blue-Green (Copper)";
            case "Btn_KCl": return "Lilac/Violet (Potassium)";
            default: return "Unknown";
        }
    }

    // --- NEW PARTICLE SYSTEM COLOR LOGIC ---
    private void ApplyParticleFlameColor(GameObject burner, string btnId)
    {
        if (burner == null) return;
        
        ParticleSystem[] allParticles = burner.GetComponentsInChildren<ParticleSystem>();
        Gradient chemGradient = new Gradient();

        switch (btnId)
        {
            case "Btn_NaCl":  // Yellow/Orange
                chemGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(1.0f, 0.7f, 0.0f), 0.5f), new GradientColorKey(new Color(1.0f, 0.3f, 0.0f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                break;
            case "Btn_LiCl":  // Crimson Red
                chemGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(1.0f, 0.1f, 0.1f), 0.5f), new GradientColorKey(new Color(0.6f, 0.0f, 0.0f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                break;
            case "Btn_CuCl2": // Blue-Green
                chemGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(0.0f, 1.0f, 0.4f), 0.5f), new GradientColorKey(new Color(0.0f, 0.4f, 0.2f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                break;
            case "Btn_KCl":  // Lilac
                chemGradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(0.7f, 0.3f, 1.0f), 0.5f), new GradientColorKey(new Color(0.4f, 0.0f, 0.8f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                break;
        }

        // Apply gradient to all child particle systems dynamically
        foreach (ParticleSystem ps in allParticles)
        {
            var col = ps.colorOverLifetime;
            col.color = chemGradient;
        }
    }

    private void ResetParticleFlameColor(GameObject burner)
    {
        if (burner == null) return;
        
        ParticleSystem[] allParticles = burner.GetComponentsInChildren<ParticleSystem>();
        
        // Restore standard Bunsen Burner Blue
        Gradient defaultGradient = new Gradient();
        defaultGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(0.0f, 0.78f, 1.0f), 0.5f), new GradientColorKey(new Color(0.2f, 0.07f, 1.0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) }
        );

        foreach (ParticleSystem ps in allParticles)
        {
            var col = ps.colorOverLifetime;
            col.color = defaultGradient;
        }
    }

    private void PlayAudio(GameObject target, AudioClip clip)
    {
        if (target == null || clip == null) return;
        AudioSource src = target.GetComponent<AudioSource>();
        if (src == null) src = target.AddComponent<AudioSource>();
        src.PlayOneShot(clip);
    }
}