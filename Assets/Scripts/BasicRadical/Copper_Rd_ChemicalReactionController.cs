using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Copper_Rd_ChemicalReactionController : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] tubeParents = new Transform[6]; 
    public GameObject dropper;
    public GameObject dropPrefab;
    public AudioSource audioSource;
    public AudioClip dripSound;
    public AudioClip swapSound; 
    public AudioClip suckSound;

    [Header("Solid Reagents")]
    public GameObject nailPrefab;
    public Material rustyMaterial;
    public AudioClip nailDropSound; 

    [Header("Heating & Cooling VFX")]
    public AudioClip boilingSound;
    public AudioSource loopAudioSource; 
    
    private Tween settleTween; 
    private int activeIndex = 0;
    private string selectedReagentName = "";
    private int clicksOnCurrentTube = 0; 
    private string currentTubeReagent = "";
    private GameObject currentPreparedNail; 
    
    private HashSet<Transform> heatedTubes = new HashSet<Transform>();
    
    [HideInInspector] public bool isDropping = false;

    [Header("Logic Managers")]
    public Copper_Rd_ExperimentManager experimentManager;
    [HideInInspector] public Vector3 testingPosition; 

    public Transform GetActiveTube()
    {
        if (tubeParents != null && activeIndex < tubeParents.Length && tubeParents[activeIndex] != null)
            return tubeParents[activeIndex];
        return null;
    }

    public string GetSelectedReagent() 
    { 
        return selectedReagentName; 
    }

    public void SetReagentType(string reagentName)
    {
        selectedReagentName = reagentName;
        if (currentPreparedNail != null)
        {
            Destroy(currentPreparedNail);
            currentPreparedNail = null;
        }
    }

    public void PrepareNailAboveTube()
    {
        Transform targetTube = tubeParents[activeIndex];
        
        currentPreparedNail = Instantiate(nailPrefab, targetTube); 
        
        currentPreparedNail.transform.localScale = new Vector3(140.98f, 141.69f, 140.98f);
        currentPreparedNail.transform.localRotation = Quaternion.Euler(28.551f, 90f, -90f);

        currentPreparedNail.transform.localPosition = new Vector3(0f, 0f, 12.15f);

        Transform rustyNailChild = currentPreparedNail.transform.Find("NailRust");
        if (rustyNailChild != null) rustyNailChild.gameObject.SetActive(false);
    }

    private Color GetSupernatantColor(string reagent, int clicks)
    {
        Color startingBlue = new Color(0.0f, 0.63f, 1.0f, 0.3f);

        if (reagent == "Btn_Fe") 
        {
            // NEW: A much more realistic, dirty yellow-green color for FeSO4
            return new Color(0.65f, 0.85f, 0.3f, 0.4f); 
        }
        
        if (reagent == "Btn_K4FeCN6") 
        {
            Color colorless = new Color(0.9f, 0.9f, 0.9f, 0.05f); 
            Color excessYellow = new Color(1.0f, 0.95f, 0.4f, 0.25f); 

            if (clicks <= 5)
            {
                float transitionTarget = clicks / 5.0f;
                return Color.Lerp(startingBlue, colorless, transitionTarget);
            }
            else
            {
                float transitionTarget = (clicks - 5) / 5.0f; 
                return Color.Lerp(colorless, excessYellow, transitionTarget);
            }
        }

        return startingBlue; 
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
                TriggerSettling(currentTube, GetSupernatantColor(currentTubeReagent, clicksOnCurrentTube), sedimentAmount);
            }

            Sequence swapSeq = DOTween.Sequence();
            swapSeq.AppendInterval(1.0f);
            swapSeq.Append(currentTube.DOMoveY(0.25f, 0.4f).SetRelative().SetEase(Ease.OutSine)); 
            swapSeq.Append(currentTube.DOMove(currentTube.parent.position + (Vector3.up * 0.25f), 0.6f).SetEase(Ease.InOutSine)); 
            swapSeq.Append(currentTube.DOMove(currentTube.parent.position, 0.4f).SetEase(Ease.InSine)); 
            
            swapSeq.AppendCallback(() => {
                if (audioSource != null && swapSound != null) audioSource.PlayOneShot(swapSound);
            });
            
            swapSeq.AppendCallback(() => {
                activeIndex++;
                clicksOnCurrentTube = 0; 
                currentTubeReagent = ""; 
                
                Transform newTube = tubeParents[activeIndex];
                
                Sequence inSeq = DOTween.Sequence();
                inSeq.Append(newTube.DOMoveY(0.25f, 0.4f).SetRelative().SetEase(Ease.OutSine)); 
                inSeq.Append(newTube.DOMove(testingPosition + (Vector3.up * 0.25f), 0.6f).SetEase(Ease.InOutSine)); 
                inSeq.Append(newTube.DOMove(testingPosition, 0.4f).SetEase(Ease.InSine)); 
                
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

        if (selectedReagentName == "Btn_Fe")
        {
            StartNailReactionSequence();
            return;
        }

        if (settleTween != null) settleTween.Kill();
        isDropping = true;
        if (clicksOnCurrentTube == 0) currentTubeReagent = selectedReagentName;

        clicksOnCurrentTube++;
        if (clicksOnCurrentTube > 10) { isDropping = false; return; }

        Transform targetTube = tubeParents[activeIndex];
        Vector3 originalRotation = dropper.transform.eulerAngles;

        Material dropperMat = null;
        Transform dropperLiquid = dropper.transform.Find("dropper_liquid");
        if (dropperLiquid != null) {
            dropperMat = dropperLiquid.GetComponent<Renderer>().material;
            Color reagentCol = GetReagentColor(selectedReagentName);
            dropperMat.SetColor("_TopColor", reagentCol);
            dropperMat.SetColor("_BaseColor", reagentCol);
            dropperMat.SetColor("_EdgeColor", reagentCol);
            dropperMat.SetFloat("_FillLevel", 0.0f); 
        }

        Material beakerMat = null;
        Transform beakerLiquid = beakerInstance.transform.Find("liquid_mesh");
        if (beakerLiquid != null) beakerMat = beakerLiquid.GetComponent<Renderer>().material;

        Vector3 beakerTopPos = beakerInstance.transform.position + (Vector3.up * 0.15f);
        Vector3 beakerInsidePos = beakerInstance.transform.position + (Vector3.up * 0.05f);

        dropper.transform.position = beakerInstance.transform.position + (Vector3.up * 0.4f);
        dropper.SetActive(true);

        Sequence s = DOTween.Sequence();
        
        s.Append(dropper.transform.DOMove(beakerTopPos, 0.4f).SetEase(Ease.InOutSine));
        s.Append(dropper.transform.DOMove(beakerInsidePos, 0.3f).SetEase(Ease.OutSine));

        s.AppendCallback(() => {
            if (audioSource != null && suckSound != null) audioSource.PlayOneShot(suckSound);
            if (dropperMat != null) dropperMat.DOFloat(0.75f, "_FillLevel", 0.5f);
            
            if (beakerMat != null) {
                float currentFill = beakerMat.HasProperty("_FillLevel") ? beakerMat.GetFloat("_FillLevel") : 0.5f;
                beakerMat.DOFloat(currentFill - 0.05f, "_FillLevel", 0.5f);
            }
        });
        
        s.AppendInterval(0.6f); 

        s.Append(dropper.transform.DOMove(beakerTopPos, 0.3f).SetEase(Ease.InSine));
        s.Append(dropper.transform.DOMove(targetTube.position + Vector3.up * 0.25f, 0.8f).SetEase(Ease.InOutQuad));
        s.Append(dropper.transform.DORotate(new Vector3(0, 0, 35f), 0.3f, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine));
        s.Join(dropper.transform.DOMove(targetTube.position + Vector3.up * 0.18f, 0.3f));

        float dropInterval = (clicksOnCurrentTube == 1) ? 0.9f : 0.4f;

        for (int i = 0; i < 3; i++)
        {
            int dropIndex = i;
            s.AppendCallback(() => {
                Vector3 dropStartPos = dropper.transform.position + (Vector3.down * 0.05f);
                SpawnDrop(dropStartPos, targetTube, dropIndex); 
                
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
            if (dropperMat != null) dropperMat.SetFloat("_FillLevel", 0f);
            
            // --- NEW TRACKING LOGIC FOR LIQUIDS ---
            if (experimentManager != null)
            {
                if (selectedReagentName == "Btn_HCl" && clicksOnCurrentTube >= 1) 
                    experimentManager.testedHCl = true;
                    
                if (selectedReagentName == "Btn_H2S" && clicksOnCurrentTube >= 1) 
                    experimentManager.testedH2S = true;
                    
                if (selectedReagentName == "Btn_K4FeCN6" && clicksOnCurrentTube >= 1) 
                    experimentManager.testedK4FeCN6 = true;

                experimentManager.CheckExperimentSuccess();
            }
            // --------------------------------------
            
            if (clicksOnCurrentTube == 10)
            {
                if (selectedReagentName == "Btn_NaOH") {
                    if (experimentManager != null) experimentManager.ShowHeatButton();
                } else {
                    if (experimentManager != null) experimentManager.HideAddButton();
                }

                TestTubeData data = targetTube.GetComponent<TestTubeData>();
                if (data != null) data.isUsed = true;
            }
        });
    }

    private void StartNailReactionSequence()
    {
        if (settleTween != null) settleTween.Kill();
        isDropping = true;
        clicksOnCurrentTube = 10;
        
        // FIX: Tell the script we are actively using Iron so SwapTube knows what color to save!
        currentTubeReagent = "Btn_Fe"; 

        // FIX: Hide the ADD button immediately upon clicking it
        if (experimentManager != null) experimentManager.HideAddButton();

        Transform targetTube = tubeParents[activeIndex];
        GameObject nailInstance = currentPreparedNail; 
        currentPreparedNail = null; 
        
        Transform singleNail = nailInstance.transform.Find("Nail");
        Renderer nailRend = (singleNail != null) ? singleNail.GetComponent<Renderer>() : nailInstance.GetComponent<Renderer>();

        Material originalShinyMat = null;
        Material animatedMat = null;
        if (nailRend != null)
        {
            originalShinyMat = nailRend.material;
            animatedMat = new Material(originalShinyMat);
            nailRend.material = animatedMat;
        }

        TestTubeData tubeData = targetTube.GetComponent<TestTubeData>();
        Material liquidMat = null;
        if (tubeData != null && tubeData.liquidMesh != null)
        {
            liquidMat = tubeData.liquidMesh.GetComponent<Renderer>().material;
            liquidMat.SetColor("_PrecipitateColor", new Color(0.6f, 0.3f, 0.1f, 1f)); 
            
            // FIX: Set the cloud to zero *before* dropping so the surface doesn't flash red!
            liquidMat.SetFloat("_CloudTop", 0f); 
            liquidMat.SetFloat("_CloudBottom", 0f); 
        }

        Sequence s = DOTween.Sequence();
        
        s.Append(nailInstance.transform.DOLocalMove(new Vector3(0f, 0f, 1.9f), 1.5f).SetEase(Ease.InCubic));
        
        s.AppendCallback(() => {
            if (experimentManager != null) experimentManager.UpdateStatusText("Reacting", "Iron displacing Copper (Wait 1 min)...");
            
            if (audioSource != null && nailDropSound != null) audioSource.PlayOneShot(nailDropSound);

            if (liquidMat != null)
            {
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.5f;
                liquidMat.DOFloat(currentFill + 0.1f, "_FillLevel", 0.5f).SetEase(Ease.OutQuad);

                float currentWobble = liquidMat.HasProperty("_WobbleAmplitude") ? liquidMat.GetFloat("_WobbleAmplitude") : 0.02f;
                liquidMat.DOFloat(0.1f, "_WobbleAmplitude", 0.3f).SetLoops(4, LoopType.Yoyo).OnComplete(() => {
                    liquidMat.DOFloat(currentWobble, "_WobbleAmplitude", 0.5f);
                });

                Color paleGreen = GetSupernatantColor("Btn_Fe", 10);
                liquidMat.DOColor(paleGreen, "_LiquidColor", 30f);
                liquidMat.DOColor(paleGreen, "_ShallowColor", 30f);
                liquidMat.DOColor(paleGreen, "_SurfaceColor", 30f);
            }
        });

        s.AppendInterval(30f);
        
        s.AppendCallback(() => {
            if (experimentManager != null) experimentManager.UpdateStatusText("Reacting", "Copper precipitate forming at bottom...");
            
            if (liquidMat != null)
            {
                liquidMat.DOFloat(0.15f, "_CloudTop", 5f);
            }
        });

        s.AppendInterval(30f);

        s.AppendCallback(() => {
            if (experimentManager != null) experimentManager.UpdateStatusText("Finished", "Reddish-brown copper deposited on the nail.");

            if (animatedMat != null && rustyMaterial != null)
            {
                DOVirtual.Float(0f, 1f, 3f, (float t) => {
                    animatedMat.Lerp(originalShinyMat, rustyMaterial, t);
                }).SetEase(Ease.InOutSine).OnComplete(() => {
                    if (nailRend != null) nailRend.material = rustyMaterial;
                });
            }
        });

        s.AppendInterval(3.5f);
        
        s.OnComplete(() => {
            isDropping = false;
            nailInstance.transform.SetParent(targetTube);

            if (tubeData != null) tubeData.isUsed = true;
            
            // --- NEW TRACKING LOGIC FOR IRON NAIL ---
            if (experimentManager != null)
            {
                experimentManager.testedFe = true;
                experimentManager.CheckExperimentSuccess();
            }
            // ----------------------------------------
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
            case "Btn_HCl":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0,0,0,0), "No precipitate forms.");
                break;
            case "Btn_H2S":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.1f, 0.1f, 0.1f, 1f), "Black precipitate of CuS forms.");
                break;
            case "Btn_NaOH":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.4f, 0.7f, 0.9f, 0.9f), "Pale blue Cu(OH)2 ppt forms.");
                break;
            case "Btn_Fe":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.6f, 0.3f, 0.1f, 1f), "Reddish-brown copper deposits.");
                break;
            case "Btn_K4FeCN6": 
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.4f, 0.2f, 0.1f, 1f), "Chocolate brown precipitate forms.");
                break;
        }
    }

    public void ApplyHeatToTube(Transform tube)
    {
        TestTubeData data = tube.GetComponent<TestTubeData>();
        if (data == null || heatedTubes.Contains(tube)) return;

        heatedTubes.Add(tube);
        
        // --- NEW TRACKING LOGIC FOR NaOH ---
        if (experimentManager != null)
        {
            experimentManager.heatedNaOH = true;
            experimentManager.CheckExperimentSuccess();
        }
        // -----------------------------------
        
        if (experimentManager != null) 
            experimentManager.UpdateStatusText("Heating", "Decomposing Cu(OH)2...");

        if (loopAudioSource != null && boilingSound != null) {
            loopAudioSource.clip = boilingSound;
            loopAudioSource.loop = true;
            loopAudioSource.volume = 1f; 
            loopAudioSource.Play();
        }

        Material mat = data.liquidMesh.GetComponent<Renderer>().material;
        
        float startWobbleAmp = mat.HasProperty("_WobbleAmplitude") ? mat.GetFloat("_WobbleAmplitude") : 0.02f;
        float startWobbleSpeed = mat.HasProperty("_WobbleSpeed") ? mat.GetFloat("_WobbleSpeed") : 2.5f;

        mat.DOFloat(0.1f, "_WobbleAmplitude", 2f);
        mat.DOFloat(10f, "_WobbleSpeed", 2f);

        Color blackCuO = new Color(0.1f, 0.1f, 0.1f, 1f);

        Sequence heatingSeq = DOTween.Sequence();
        heatingSeq.Append(mat.DOColor(blackCuO, "_PrecipitateColor", 4f));

        heatingSeq.OnComplete(() => {
            if (loopAudioSource != null) loopAudioSource.Stop();
            
            mat.DOFloat(startWobbleAmp, "_WobbleAmplitude", 2f);
            mat.DOFloat(startWobbleSpeed, "_WobbleSpeed", 2f);

            if (experimentManager != null) 
                experimentManager.UpdateStatusText("Finished", "Black CuO precipitate has formed.");
        });
    }

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

    private void HandleReaction_Generic(Renderer liquidRend, Transform targetTube, int currentClickPhase, int dropIndex, Color pptColor, string message)
    {
        Material liquidMat = liquidRend.material;

        if (currentClickPhase == 1)
        {
            if (dropIndex == 0) {
                liquidMat.SetColor("_PrecipitateColor", pptColor);
                liquidMat.SetFloat("_CloudTop", 1.0f); 
                liquidMat.SetFloat("_CloudBottom", 0.95f); 
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 1", message);
            }
            else if (dropIndex == 1) liquidMat.DOFloat(0.8f, "_CloudBottom", 0.5f);
            else if (dropIndex == 2) liquidMat.DOFloat(0.6f, "_CloudBottom", 0.5f);
        }
        else if (currentClickPhase >= 2 && currentClickPhase <= 5)
        {
            if (dropIndex == 0) {
                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Reaction intensifying...");
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 3", "Adding excess reagent...");
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.DOFloat(1.0f, "_CloudTop", 1.5f).SetEase(Ease.OutSine);
                liquidMat.DOFloat(0.0f, "_CloudBottom", 1.5f).SetEase(Ease.OutSine);
            }
        }

        float sedimentAmount = GetSedimentHeight(selectedReagentName, currentClickPhase);
        ResetSettleTimer(targetTube, GetSupernatantColor(selectedReagentName, currentClickPhase), sedimentAmount);
    }

    private float GetSedimentHeight(string reagent, int clicks)
    {
        if (reagent == "Btn_HCl") return 0.0f; 
        if (reagent == "Btn_Fe") return 0.15f; 
        return Mathf.Lerp(0.10f, 0.50f, clicks / 10f);
    }

    public Color GetReagentColor(string reagent)
    {
        if (reagent == "Btn_K4FeCN6") return new Color(1.0f, 0.9f, 0.1f, 0.9f); 
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
            mat.SetFloat("_FillLevel", 0.5f); 
        }
    }
}