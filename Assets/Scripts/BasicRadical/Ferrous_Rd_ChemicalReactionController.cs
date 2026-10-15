using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Ferrous_Rd_ChemicalReactionController : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] tubeParents = new Transform[6]; 
    public GameObject dropper;
    public GameObject dropPrefab;
    public AudioSource audioSource;
    public AudioClip dripSound;
    public AudioClip swapSound; 
    public AudioClip suckSound;
    
    private Tween settleTween; 
    private int activeIndex = 0;
    private string selectedReagentName = "";
    private int clicksOnCurrentTube = 0; 
    private string currentTubeReagent = "";
    
    [HideInInspector] public bool isDropping = false;

    [Header("Logic Managers")]
    public Ferrous_Rd_ExperimentManager experimentManager;
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

    private Color GetSupernatantColor(string reagent, int clicks)
    {
        Color baseGreen = new Color(0.4f, 0.8f, 0.4f, 0.6f);
        Color palerGreen = new Color(0.45f, 0.85f, 0.45f, 0.55f);
        return Color.Lerp(baseGreen, palerGreen, clicks / 10f);
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
            
            // --- NEW TRACKING LOGIC ---
            if (experimentManager != null)
            {
                if (selectedReagentName == "Btn_HCl" && clicksOnCurrentTube >= 1) experimentManager.testedHCl = true;
                if (selectedReagentName == "Btn_H2S" && clicksOnCurrentTube >= 1) experimentManager.testedH2S = true;
                if (selectedReagentName == "Btn_Buffer" && clicksOnCurrentTube >= 1) experimentManager.testedBuffer = true;
                if (selectedReagentName == "Btn_K3FeCN6" && clicksOnCurrentTube >= 1) experimentManager.testedK3FeCN6 = true;
                if (selectedReagentName == "Btn_NH42S" && clicksOnCurrentTube >= 1) experimentManager.testedNH42S = true;

                experimentManager.CheckExperimentSuccess();
            }
            // --------------------------
            
            if (clicksOnCurrentTube == 10)
            {
                if (experimentManager != null) experimentManager.HideAddButton();

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
            case "Btn_HCl":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0,0,0,0), "No precipitate forms.");
                break;
            case "Btn_H2S":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0,0,0,0), "No precipitate forms in acidic medium.");
                break;
            case "Btn_Buffer":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.3f, 0.4f, 0.2f, 0.9f), "Dirty green Fe(OH)2 ppt forms.");
                break;
            case "Btn_K3FeCN6":
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.0f, 0.1f, 0.5f, 1.0f), "Deep blue Turnbull's ppt forms.");
                break;
            case "Btn_NH42S": 
                HandleReaction_Generic(liquidRend, targetTube, currentClickPhase, dropIndex, new Color(0.1f, 0.1f, 0.1f, 1.0f), "Black FeS precipitate forms.");
                break;
        }
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
            
            // Push the bottom to -0.1f to prevent visual gaps at the base of the mesh
            seq.Join(mat.DOFloat(-0.1f, "_CloudBottom", 5f).SetEase(Ease.InOutSine));
            
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
                liquidMat.SetFloat("_CloudTop", 1.0f); 
                liquidMat.DOFloat(0.66f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 2", "Reaction intensifying...");
            }
            else if (dropIndex == 1) {
                liquidMat.DOFloat(0.33f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
            }
            else if (dropIndex == 2) {
                liquidMat.DOFloat(-0.1f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
            }
        }
        else if (currentClickPhase > 5 && currentClickPhase <= 10)
        {
            if (dropIndex == 0) {
                if (experimentManager != null) experimentManager.UpdateStatusText("Phase 3", "Adding excess reagent...");
                float currentFill = liquidMat.HasProperty("_FillLevel") ? liquidMat.GetFloat("_FillLevel") : 0.514f;
                float targetFill = Mathf.Min(currentFill + ((0.8f - 0.514f) / 5f), 0.8f);
                liquidMat.DOFloat(targetFill, "_FillLevel", 1.5f);

                liquidMat.SetFloat("_CloudTop", 1.0f); 
                liquidMat.DOFloat(0.66f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
            }
            else if (dropIndex == 1) {
                liquidMat.DOFloat(0.33f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
            }
            else if (dropIndex == 2) {
                liquidMat.DOFloat(-0.1f, "_CloudBottom", 0.4f).SetEase(Ease.OutSine);
            }
        }

        float sedimentAmount = GetSedimentHeight(selectedReagentName, currentClickPhase);
        
        // UPDATED: Pass the current clicks to calculate the exact color during the drop animation
        ResetSettleTimer(targetTube, GetSupernatantColor(selectedReagentName, currentClickPhase), sedimentAmount);
    }

    private float GetSedimentHeight(string reagent, int clicks)
    {
        if (reagent == "Btn_HCl" || reagent == "Btn_H2S") return 0.0f; 
        return Mathf.Lerp(0.10f, 0.50f, clicks / 10f);
    }

    public Color GetReagentColor(string reagent)
    {
        if (reagent == "Btn_K3FeCN6") return new Color(1.0f, 0.9f, 0.1f, 0.9f); 
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