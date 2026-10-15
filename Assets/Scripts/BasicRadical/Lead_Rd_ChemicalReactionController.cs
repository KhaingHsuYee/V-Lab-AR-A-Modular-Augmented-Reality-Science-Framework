using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Lead_Rd_ChemicalReactionController : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] tubeParents = new Transform[6]; 
    public GameObject dropper;
    public GameObject dropPrefab;
    public AudioSource audioSource;
    public AudioClip dripSound;
    public AudioClip swapSound;  // Glass clink/thud when it lands
    public AudioClip suckSound;

    [Header("VFX")]
    public GameObject reactionParticlePrefab; 
    
    [Header("Heating & Cooling VFX")]
    public AudioClip boilingSound;
    public AudioSource loopAudioSource; // Dedicated AudioSource for boiling
    public GameObject goldenRainParticlePrefab;
    
    private GameObject currentHeatingRing;
    private Tween settleTween; 

    private int activeIndex = 0;
    private string selectedReagentName = "";
    private int clicksOnCurrentTube = 0; 
    private string currentTubeReagent = "";
    
    // Trackers to avoid needing TestTubeData edits
    private HashSet<Transform> heatedTubes = new HashSet<Transform>();
    private HashSet<Transform> goldenRainTubes = new HashSet<Transform>();
    
    [HideInInspector] public bool isDropping = false;

    [Header("Logic Managers")]
    public Lead_Rd_ExperimentManager experimentManager;
    [HideInInspector] public Vector3 testingPosition; 

    public Transform GetActiveTube()
    {
        if (tubeParents != null && activeIndex < tubeParents.Length && tubeParents[activeIndex] != null)
            return tubeParents[activeIndex];
        return null;
    }

    public void SetReagentType(string reagentName)
    {
        selectedReagentName = reagentName;
    }

    private Color GetSupernatantColor(string reagent)
    {
        if (reagent == "Btn_KI") return new Color(1.0f, 0.85f, 0.15f, 0.45f); 
        if (reagent == "Btn_K2CrO4") return new Color(1.0f, 0.9f, 0.2f, 0.4f); 
        return new Color(0.8f, 0.9f, 0.95f, 0.1f); 
    }

    public void SwapTubeIfNeeded(string newReagentName, System.Action<Transform> onSwapComplete)
    {
        Transform currentTube = tubeParents[activeIndex];
        
        bool isFull = clicksOnCurrentTube >= 10;
        bool isDirtyWithDifferentReagent = (clicksOnCurrentTube > 0 && currentTubeReagent != newReagentName);

        if (isFull || isDirtyWithDifferentReagent)
        {
            if (activeIndex >= 5)
            {
               if (experimentManager != null) experimentManager.ShowOutOfTubesPopup();
                onSwapComplete?.Invoke(null);
                return;
            }
            
            if (clicksOnCurrentTube > 0) 
            {
                if (settleTween != null) settleTween.Kill();
                float sedimentAmount = GetSedimentHeight(currentTubeReagent, clicksOnCurrentTube);
                TriggerSettling(currentTube, GetSupernatantColor(currentTubeReagent), sedimentAmount);
            }

            Sequence swapSeq = DOTween.Sequence();
            swapSeq.AppendInterval(1.0f);
            
            // 1. Old tube lifts up
            swapSeq.Append(currentTube.DOMoveY(0.25f, 0.4f).SetRelative().SetEase(Ease.OutSine)); 
            
            // 2. Old tube moves horizontal
            swapSeq.Append(currentTube.DOMove(currentTube.parent.position + (Vector3.up * 0.25f), 0.6f).SetEase(Ease.InOutSine)); 
            
            // 3. Old tube goes down into the rack
            swapSeq.Append(currentTube.DOMove(currentTube.parent.position, 0.4f).SetEase(Ease.InSine)); 
            
            // 4. Play Clink (Landing in the rack)
            swapSeq.AppendCallback(() => {
                if (audioSource != null && swapSound != null) audioSource.PlayOneShot(swapSound);
            });
            
            swapSeq.AppendCallback(() => {
                activeIndex++;
                clicksOnCurrentTube = 0; 
                currentTubeReagent = ""; 
                
                Transform newTube = tubeParents[activeIndex];
                
                Sequence inSeq = DOTween.Sequence();
                
                // 1. New tube lifts up from the rack
                inSeq.Append(newTube.DOMoveY(0.25f, 0.4f).SetRelative().SetEase(Ease.OutSine)); 
                
                // 2. New tube moves horizontal to testing position
                inSeq.Append(newTube.DOMove(testingPosition + (Vector3.up * 0.25f), 0.6f).SetEase(Ease.InOutSine)); 
                
                // 3. New tube goes down to testing position
                inSeq.Append(newTube.DOMove(testingPosition, 0.4f).SetEase(Ease.InSine)); 
                
                // 4. Play Clink (Landing on the testing spot)
                inSeq.AppendCallback(() => {
                    if (audioSource != null && swapSound != null) audioSource.PlayOneShot(swapSound);
                });
                
                inSeq.OnComplete(() => {
                    onSwapComplete?.Invoke(newTube); 
                });
            });
        }
        else
        {
            onSwapComplete?.Invoke(currentTube);
        }
    }

    public void StartReactionSequence(GameObject beakerInstance)
    {
        if (tubeParents == null || tubeParents.Length == 0 || tubeParents[activeIndex] == null) return;
        if (isDropping) return;

        if (settleTween != null) settleTween.Kill();
        isDropping = true;
        if (clicksOnCurrentTube == 0) currentTubeReagent = selectedReagentName;

        clicksOnCurrentTube++;
        if (clicksOnCurrentTube > 10) { isDropping = false; return; }

        Transform targetTube = tubeParents[activeIndex];
        Vector3 originalRotation = dropper.transform.eulerAngles;

        // Find Materials for Dropper and Beaker
        Material dropperMat = null;
        Transform dropperLiquid = dropper.transform.Find("dropper_liquid");
        if (dropperLiquid != null) {
            dropperMat = dropperLiquid.GetComponent<Renderer>().material;
            Color reagentCol = GetReagentColor(selectedReagentName);
            dropperMat.SetColor("_TopColor", reagentCol);
            dropperMat.SetColor("_BaseColor", reagentCol);
            dropperMat.SetColor("_EdgeColor", reagentCol);
            dropperMat.SetFloat("_FillLevel", 0.0f); // Start empty
        }

        Material beakerMat = null;
        Transform beakerLiquid = beakerInstance.transform.Find("liquid_mesh");
        if (beakerLiquid != null) beakerMat = beakerLiquid.GetComponent<Renderer>().material;

        Vector3 beakerTopPos = beakerInstance.transform.position + (Vector3.up * 0.15f);
        Vector3 beakerInsidePos = beakerInstance.transform.position + (Vector3.up * 0.05f);

        // Position dropper high above beaker to start
        dropper.transform.position = beakerInstance.transform.position + (Vector3.up * 0.4f);
        dropper.SetActive(true);

        Sequence s = DOTween.Sequence();
        
        // 1. Move down to Beaker
        s.Append(dropper.transform.DOMove(beakerTopPos, 0.4f).SetEase(Ease.InOutSine));
        s.Append(dropper.transform.DOMove(beakerInsidePos, 0.3f).SetEase(Ease.OutSine));

        // 2. Suck the Liquid
        s.AppendCallback(() => {
            if (audioSource != null && suckSound != null) audioSource.PlayOneShot(suckSound);
            
            // THE FIX: Fill dropper to exactly 75% so 3 drops (25% each) drain it perfectly!
            if (dropperMat != null) dropperMat.DOFloat(0.75f, "_FillLevel", 0.5f);
            
            // Drain beaker slightly
            if (beakerMat != null) {
                float currentFill = beakerMat.HasProperty("_FillLevel") ? beakerMat.GetFloat("_FillLevel") : 0.5f;
                beakerMat.DOFloat(currentFill - 0.05f, "_FillLevel", 0.5f);
            }
        });
        
        s.AppendInterval(0.6f); // Wait while sucking completes

        // 3. Move out of Beaker and over to Test Tube
        s.Append(dropper.transform.DOMove(beakerTopPos, 0.3f).SetEase(Ease.InSine));
        s.Append(dropper.transform.DOMove(targetTube.position + Vector3.up * 0.25f, 0.8f).SetEase(Ease.InOutQuad));
        s.Append(dropper.transform.DORotate(new Vector3(0, 0, 35f), 0.3f, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine));
        s.Join(dropper.transform.DOMove(targetTube.position + Vector3.up * 0.18f, 0.3f));

        float dropInterval = (clicksOnCurrentTube == 1) ? 0.9f : 0.4f;
        
        // 4. Dispense Drops
        for (int i = 0; i < 3; i++)
        {
            int dropIndex = i;
            s.AppendCallback(() => {
                Vector3 dropStartPos = dropper.transform.position + (Vector3.down * 0.05f);
                SpawnDrop(dropStartPos, targetTube, dropIndex); 
                
                // THE FIX: Calculate the exact target level instead of relying on the current float value.
                if (dropperMat != null) {
                    float targetLevel = 0.75f - ((dropIndex + 1) * 0.25f);
                    dropperMat.DOFloat(targetLevel, "_FillLevel", 0.1f);
                }
            });
            s.AppendInterval(dropInterval);
        }

        s.OnComplete(() => {
            dropper.transform.eulerAngles = originalRotation; 
            dropper.SetActive(false);
            isDropping = false; 
            if (dropperMat != null) dropperMat.SetFloat("_FillLevel", 0f); // Reset to 0
            
            // --- NEW TRACKING LOGIC ---
            if (experimentManager != null)
            {
                if (selectedReagentName == "Btn_HCl" && clicksOnCurrentTube >= 1) 
                    experimentManager.testedHCl = true;
                    
                if (selectedReagentName == "Btn_K2CrO4" && clicksOnCurrentTube >= 1) 
                    experimentManager.testedK2CrO4 = true;
                    
                if (selectedReagentName == "Btn_NaOH" && clicksOnCurrentTube > 5) 
                    experimentManager.excessNaOH = true;

                experimentManager.CheckExperimentSuccess();
            }
            // --------------------------
            
            if (clicksOnCurrentTube == 10)
            {
                if (selectedReagentName == "Btn_KI") {
                    if (experimentManager != null) experimentManager.ShowHeatButton();
                } else {
                    if (experimentManager != null) experimentManager.HideAddButton();
                }

                TestTubeData data = targetTube.GetComponent<TestTubeData>();
                if (data != null) data.isUsed = true;
            }
        });
    }

    private void SpawnDrop(Vector3 startPos, Transform targetTube, int dropIndex)
    {
        if (audioSource != null && dripSound != null) audioSource.PlayOneShot(dripSound);

        float surfaceY = startPos.y - 0.15f; 
        TestTubeData tubeData = targetTube.GetComponent<TestTubeData>();
        
        if (tubeData != null && tubeData.liquidMesh != null)
        {
            Renderer liquidRend = tubeData.liquidMesh.GetComponent<Renderer>();
            Material mat = liquidRend.material;

            if (mat.HasProperty("_FillLevel"))
            {
                float fill = mat.GetFloat("_FillLevel");
                float meshBottomY = liquidRend.bounds.min.y;
                float meshTopY = liquidRend.bounds.max.y;
                surfaceY = Mathf.Lerp(meshBottomY, meshTopY, fill);
            }
        }

        GameObject drop = Instantiate(dropPrefab, startPos, Quaternion.identity);
        Collider col = drop.GetComponent<Collider>();
        if (col != null) Destroy(col);

        drop.transform.DOMoveY(surfaceY, 0.35f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(drop);
            
            if (tubeData != null && tubeData.liquidMesh != null)
            {
                Renderer actualLiquidRend = tubeData.liquidMesh.GetComponent<Renderer>();
                ApplyReagentReaction(actualLiquidRend, targetTube, clicksOnCurrentTube, dropIndex, new Vector3(startPos.x, surfaceY, startPos.z));
            }
        });
    }

    private void ApplyReagentReaction(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Vector3 surfacePos)
    {
        switch (selectedReagentName)
        {
            case "Btn_KI":
                HandleReaction_KI(liquidRend, targetTube, currentClickPhase, dropIndex, surfacePos);
                break;
            case "Btn_HCl":
                HandleReaction_HCl(liquidRend, targetTube, currentClickPhase, dropIndex, surfacePos);
                break;
            case "Btn_NaOH":
                HandleReaction_NaOH(liquidRend, targetTube, currentClickPhase, dropIndex, surfacePos);
                break;
            case "Btn_K2CrO4": 
                HandleReaction_K2CrO4(liquidRend, targetTube, currentClickPhase, dropIndex, surfacePos);
                break;
        }
    }

    // ==========================================
    // THE HEATING & GOLDEN RAIN SEQUENCE
    // ==========================================

    public void ApplyHeatToTube(Transform tube)
    {
        TestTubeData data = tube.GetComponent<TestTubeData>();
        if (data == null || heatedTubes.Contains(tube)) return;

        heatedTubes.Add(tube);
        
        // --- NEW TRACKING LOGIC FOR KI ---
        if (experimentManager != null)
        {
            experimentManager.heatedKI = true;
            experimentManager.CheckExperimentSuccess();
        }
        // ---------------------------------
        
        if (experimentManager != null) 
            experimentManager.UpdateStatusText("Heating", "Boiling Lead(II) Iodide...");

        // 1. Play Boiling Sound (Forcing volume to 1.0)
        if (loopAudioSource != null && boilingSound != null) {
            loopAudioSource.clip = boilingSound;
            loopAudioSource.loop = true;
            loopAudioSource.volume = 1f; // Forces volume to maximum in script
            loopAudioSource.Play();
        }

        Material mat = data.liquidMesh.GetComponent<Renderer>().material;
        
        // 2. Initiate Shader Wobble (Boiling Effect)
        float startWobbleAmp = mat.HasProperty("_WobbleAmplitude") ? mat.GetFloat("_WobbleAmplitude") : 0.02f;
        float startWobbleSpeed = mat.HasProperty("_WobbleSpeed") ? mat.GetFloat("_WobbleSpeed") : 2.5f;

        mat.DOFloat(0.1f, "_WobbleAmplitude", 2f);
        mat.DOFloat(10f, "_WobbleSpeed", 2f);

        // 3. Color Transition logic
        Color transparentYellow = new Color(1f, 0.816f, 0f, 0.4f);
        Color clearColor = new Color(0.8f, 0.9f, 0.95f, 0.1f);
        Color invisibleCloud = new Color(1f, 0.816f, 0f, 0f);

        Sequence heatingSeq = DOTween.Sequence();
        
        // Milky -> Transparent Yellow
        heatingSeq.Append(mat.DOColor(transparentYellow, "_PrecipitateColor", 4f));
        heatingSeq.Join(mat.DOFloat(0.8f, "_LiquidDensity", 4f));
        
        // Transparent Yellow -> Colorless
        heatingSeq.Append(mat.DOColor(invisibleCloud, "_PrecipitateColor", 4f));
        heatingSeq.Join(mat.DOColor(clearColor, "_LiquidColor", 4f));
        heatingSeq.Join(mat.DOColor(clearColor, "_ShallowColor", 4f));
        heatingSeq.Join(mat.DOFloat(0.2f, "_LiquidDensity", 4f));

        heatingSeq.OnComplete(() => {
            
            // 4. Heating Stopped
            if (loopAudioSource != null) loopAudioSource.Stop();
            
            mat.DOFloat(startWobbleAmp, "_WobbleAmplitude", 2f);
            mat.DOFloat(startWobbleSpeed, "_WobbleSpeed", 2f);

            if (experimentManager != null) 
                experimentManager.UpdateStatusText("Cooling", "Heating complete. Cooling down for 30 seconds...");

            // 5. 30 Second Cooldown Timer starts here!
            DOVirtual.DelayedCall(30f, () => TriggerGoldenRain(tube));
        });
    }

    private void TriggerGoldenRain(Transform tube)
    {
        TestTubeData data = tube.GetComponent<TestTubeData>();
        if (data == null || goldenRainTubes.Contains(tube)) return;

        goldenRainTubes.Add(tube);
        
        if (experimentManager != null) 
            experimentManager.UpdateStatusText("Golden Rain", "PbI2 recrystallizing into golden flakes!");

        // ---> 1. JUST WAKE UP THE INACTIVE PARTICLES <---
        // The 'true' parameter tells Unity to search for inactive objects too!
        ParticleSystem ps = tube.GetComponentInChildren<ParticleSystem>(true);
        if (ps != null)
        {
            ps.gameObject.SetActive(true); // Turn the GameObject on
            ps.Play(); // Start the golden rain!
            
            // Optional: Tell it to stop emitting new particles after 60 seconds
            DOVirtual.DelayedCall(60f, () => ps.Stop()); 
        }

        // ---> 2. SHADER SEDIMENT REBUILD <---
        Material mat = data.liquidMesh.GetComponent<Renderer>().material;
        Color solidYellow = new Color(1f, 0.816f, 0f, 1f);

        float finalSedimentHeight = GetSedimentHeight("Btn_KI", clicksOnCurrentTube);
        
        mat.SetFloat("_CloudTop", finalSedimentHeight);
        mat.SetFloat("_CloudBottom", 0.0f);
        
        mat.DOColor(solidYellow, "_PrecipitateColor", 60f).SetEase(Ease.InQuad).OnComplete(() => {
            if (experimentManager != null) 
                experimentManager.UpdateStatusText("Finished", "Golden crystals have settled at the bottom.");
        });
    }

    // ==========================================
    // REAGENT SPECIFIC LOGIC
    // ==========================================

    private void ResetSettleTimer(Transform tube, Color supernatantColor, float finalSedimentHeight)
    {
        if (settleTween != null) settleTween.Kill();
        settleTween = DOVirtual.DelayedCall(60f, () => {
            if (tube != null)
            {
                TriggerSettling(tube, supernatantColor, finalSedimentHeight);
                if (experimentManager != null) 
                    experimentManager.UpdateStatusText("Resting", "Precipitate has settled at the bottom.");
            }
        });
    }

    private void TriggerSettling(Transform tube, Color supernatantColor, float finalSedimentHeight)
    {
        TestTubeData data = tube.GetComponent<TestTubeData>();
        if (data != null && data.liquidMesh != null)
        {
            Material mat = data.liquidMesh.GetComponent<Renderer>().material;
            
            Sequence seq = DOTween.Sequence();
            seq.Join(mat.DOFloat(finalSedimentHeight, "_CloudTop", 5f).SetEase(Ease.InOutSine));
            seq.Join(mat.DOFloat(0.0f, "_CloudBottom", 5f).SetEase(Ease.InOutSine));
            seq.Join(mat.DOColor(supernatantColor, "_LiquidColor", 5f));
            seq.Join(mat.DOColor(supernatantColor, "_ShallowColor", 5f));
            seq.Join(mat.DOColor(supernatantColor, "_SurfaceColor", 5f));
            seq.Join(mat.DOFloat(0.8f, "_LiquidDensity", 5f));
        }
    }

    private void HandleReaction_KI(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Vector3 surfacePos)
    {
        Material liquidMat = liquidRend.material;
        Color canaryYellow = new Color(1f, 0.816f, 0f, 1f); 

        if (currentClickPhase == 1)
        {
            if (dropIndex == 0) {
                liquidMat.SetColor("_PrecipitateColor", canaryYellow);
                liquidMat.SetFloat("_CloudTop", 1.0f); 
                liquidMat.SetFloat("_CloudBottom", 0.95f); 
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 1", "Canary Blossom Plume Started.");
            }
            else if (dropIndex == 1) liquidMat.DOFloat(0.8f, "_CloudBottom", 0.5f);
            else if (dropIndex == 2) liquidMat.DOFloat(0.6f, "_CloudBottom", 0.5f);
        }
        else if (currentClickPhase >= 2 && currentClickPhase <= 5)
        {
            if (dropIndex == 0) {
                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Solution turning opaque...");
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 3", "Filling test tube to 80%...");
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
            }
        }

        float sedimentAmount = GetSedimentHeight("Btn_KI", currentClickPhase);
        ResetSettleTimer(targetTube, GetSupernatantColor("Btn_KI"), sedimentAmount);
    }

    private void HandleReaction_HCl(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Vector3 surfacePos)
    {
        Material liquidMat = liquidRend.material;
        Color chalkyWhite = new Color(0.957f, 0.961f, 0.941f, 1f); 

        if (currentClickPhase == 1)
        {
            if (dropIndex == 0) {
                liquidMat.SetColor("_PrecipitateColor", chalkyWhite);
                liquidMat.SetFloat("_CloudTop", 1.0f);
                liquidMat.SetFloat("_CloudBottom", 0.95f);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 1", "White Crystalline Precipitate Forming.");
            }
            else if (dropIndex == 1) liquidMat.DOFloat(0.8f, "_CloudBottom", 0.5f);
            else if (dropIndex == 2) liquidMat.DOFloat(0.6f, "_CloudBottom", 0.5f);
        }
        else if (currentClickPhase >= 2 && currentClickPhase <= 5)
        {
            if (dropIndex == 0) {
                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Solution turning milky white...");
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 3", "Filling test tube to 80%...");
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
            }
        }

        float sedimentAmount = GetSedimentHeight("Btn_HCl", currentClickPhase);
        ResetSettleTimer(targetTube, GetSupernatantColor("Btn_HCl"), sedimentAmount);
    }

    private void HandleReaction_NaOH(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Vector3 surfacePos)
    {
        Material liquidMat = liquidRend.material;
        Color gelWhite = new Color(0.96f, 0.96f, 0.96f, 0.85f); 

        if (currentClickPhase == 1)
        {
            if (dropIndex == 0) {
                liquidMat.SetColor("_PrecipitateColor", gelWhite);
                liquidMat.SetFloat("_CloudTop", 1.0f);
                liquidMat.SetFloat("_CloudBottom", 0.95f);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 1", "White Gelatinous Precipitate Forming.");
            }
            else if (dropIndex == 1) liquidMat.DOFloat(0.8f, "_CloudBottom", 0.5f);
            else if (dropIndex == 2) liquidMat.DOFloat(0.6f, "_CloudBottom", 0.5f);
        }
        else if (currentClickPhase >= 2 && currentClickPhase <= 5)
        {
            if (dropIndex == 0) {
                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Solution thickening with precipitate...");
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) 
                    experimentManager.UpdateStatusText("Phase 3", "Adding excess NaOH... Precipitate dissolving.");
                
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);

                int excessStep = currentClickPhase - 5; 
                float fadePercent = excessStep / 5.0f; 
                float targetAlpha = Mathf.Lerp(0.85f, 0.0f, fadePercent);
                Color targetPrecipColor = new Color(gelWhite.r, gelWhite.g, gelWhite.b, targetAlpha);
                liquidMat.DOColor(targetPrecipColor, "_PrecipitateColor", 1.5f);
            }
        }

        float sedimentAmount = GetSedimentHeight("Btn_NaOH", currentClickPhase);
        ResetSettleTimer(targetTube, GetSupernatantColor("Btn_NaOH"), sedimentAmount);
    }

    private void HandleReaction_K2CrO4(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Vector3 surfacePos)
    {
        Material liquidMat = liquidRend.material;
        Color chromeYellow = new Color(1.0f, 0.8f, 0.0f, 1.0f); 

        if (currentClickPhase == 1)
        {
            if (dropIndex == 0) {
                liquidMat.SetColor("_PrecipitateColor", chromeYellow);
                liquidMat.SetFloat("_CloudTop", 1.0f);
                liquidMat.SetFloat("_CloudBottom", 0.95f);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 1", "Bright Yellow Precipitate Forming.");
            }
            else if (dropIndex == 1) liquidMat.DOFloat(0.8f, "_CloudBottom", 0.5f);
            else if (dropIndex == 2) liquidMat.DOFloat(0.6f, "_CloudBottom", 0.5f);
        }
        else if (currentClickPhase >= 2 && currentClickPhase <= 5)
        {
            if (dropIndex == 0) {
                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Solution turning heavy opaque yellow...");
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 3", "Adding excess reagent... Filling tube.");
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
            }
        }

        float sedimentAmount = GetSedimentHeight("Btn_K2CrO4", currentClickPhase);
        ResetSettleTimer(targetTube, GetSupernatantColor("Btn_K2CrO4"), sedimentAmount);
    }

    private float GetSedimentHeight(string reagent, int clicks)
    {
        if (reagent == "Btn_NaOH")
        {
            if (clicks <= 5)
                return Mathf.Lerp(0.10f, 0.40f, clicks / 5f);
            else
            {
                float dissolvePercent = (clicks - 5) / 5f;
                return Mathf.Lerp(0.40f, 0.0f, dissolvePercent);
            }
        }
        return Mathf.Lerp(0.10f, 0.50f, clicks / 10f);
    }

    public Color GetReagentColor(string reagent)
    {
        // Potassium Chromate is Yellow
        if (reagent == "Btn_K2CrO4") return new Color(1.0f, 0.9f, 0.1f, 0.9f); 
        
        // HCl, KI, and NaOH are completely Colorless/Clear
        return new Color(0.9f, 0.95f, 1.0f, 0.2f); 
    }

    public void ApplyReagentColorToBeaker(GameObject beaker, string reagent)
    {
        Transform liquidTransform = beaker.transform.Find("liquid_mesh");
        if (liquidTransform != null)
        {
            Material mat = liquidTransform.GetComponent<Renderer>().material;
            Color col = GetReagentColor(reagent);
            mat.SetColor("_TopColor", col);
            mat.SetColor("_BaseColor", col);
            mat.SetColor("_EdgeColor", col);
            mat.SetFloat("_FillLevel", 0.5f); // Start half full
        }
    }
}