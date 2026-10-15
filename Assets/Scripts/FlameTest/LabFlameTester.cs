using UnityEngine;


using UnityEngine.VFX;


using TMPro;


using DG.Tweening;

using UnityEngine.UIElements; // Required for UIDocument and DisplayStyle


public class LabFlameTester : MonoBehaviour


{


    [Header("Managers & UI")]


    public Flame_LabChemicalManager chemicalManager;


    public GameObject pickButtonUI;


    public GameObject burnButtonUI;



    [Header("Lab Audio Clips")]


    public AudioClip saltScoopSFX;        


    public AudioClip chemicalSizzleSFX;


    [Header("New Audio")]

    public AudioClip loopDipSFX; // Add this in Inspector



    [Header("VFX Settings")]


    [Tooltip("The exact Exposed Name string found inside the VFX Graph Blackboard property window.")]


    public string vfxColorPropertyName = "_FlameColor";

    [Header("Data Assets")]
    public ElementData NaCl_Data;
    public ElementData KCl_Data;
    public ElementData LiCl_Data;
    public ElementData CuCl2_Data;


    public ARDataController uiController; // Drag your AR_UIManager here

    private ElementData currentElement;



    private GameObject liveLoopObject;    


    private GameObject saltSampleOnLoop;  


    private Vector3 loopHomePosition;    


    private Quaternion loopHomeRotation;  



    void Start()


    {


        burnButtonUI.SetActive(false);


       


        GameObject activeBurner = GameObject.FindWithTag("Burner");


        if (activeBurner != null)


        {


            VisualEffect vfxFlame = activeBurner.GetComponentInChildren<VisualEffect>();


            if (vfxFlame != null)


            {


                ResetVFXFlameColor(vfxFlame);


            }


        }


    }



    public void InitializeSpawnedLoop(GameObject spawnedLoop, Vector3 homePos)


    {


        liveLoopObject = spawnedLoop;


        loopHomePosition = homePos;


        loopHomeRotation = spawnedLoop.transform.rotation;



        Transform childTransform = liveLoopObject.transform.Find("SaltSampleOnLoop");


        if (childTransform != null)


        {


            saltSampleOnLoop = childTransform.gameObject;


            saltSampleOnLoop.SetActive(false);


        }


    }



    public void ExecutePickSample()

    {

        if (liveLoopObject == null || chemicalManager.CurrentSpawnedDish == null) return;


        pickButtonUI.SetActive(false);


        GameObject activeBurner = GameObject.FindWithTag("Burner");

        GameObject dynamicDish = chemicalManager.CurrentSpawnedDish;

        Vector3 dishPosition = dynamicDish.transform.position;

       

        // Define the "Working Position" in the air (between burner and dish)

        Vector3 workingPosition = (dishPosition + activeBurner.transform.position) / 2f + (Vector3.up * 0.15f);


        ParticleSystem dishSaltParticles = dynamicDish.GetComponentInChildren<ParticleSystem>();

        AudioSource loopAudio = liveLoopObject.GetComponent<AudioSource>();


        Sequence pickSequence = DOTween.Sequence();


        // 1. Move to dish and dip

        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition + Vector3.up * 0.06f, 0.6f));

        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition, 0.3f));

       

        pickSequence.AppendCallback(() =>

        {

            if (saltSampleOnLoop != null)

            {

                saltSampleOnLoop.SetActive(true);

                MatchSampleColor(saltSampleOnLoop, chemicalManager.SelectedChemical);

            }

            if (dishSaltParticles != null) dishSaltParticles.Play();

            if (loopAudio != null && saltScoopSFX != null) loopAudio.PlayOneShot(saltScoopSFX);

            HandleFloatingDishPopup(dynamicDish);

        });


        // 2. Lift out and move to the "Air Working Position"

        pickSequence.Append(liveLoopObject.transform.DOMove(dishPosition + Vector3.up * 0.06f, 0.3f));

        pickSequence.Append(liveLoopObject.transform.DOMove(workingPosition, 0.6f));


        pickSequence.OnComplete(() =>

        {

            burnButtonUI.SetActive(true);

        });

    }



    private void HandleFloatingDishPopup(GameObject spawnedDish)


    {


        Transform canvasTransform = spawnedDish.transform.Find("DishUICanvas");


        if (canvasTransform == null) return;



        TextMeshProUGUI floatingText = canvasTransform.GetComponentInChildren<TextMeshProUGUI>();


        if (floatingText != null)


        {


            floatingText.text = chemicalManager.IsUnknownSelected ? "Pick up salt" : $"Picked up {chemicalManager.SelectedChemical}";


        }



        canvasTransform.gameObject.SetActive(true);


        canvasTransform.LookAt(canvasTransform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);


        canvasTransform.localScale = Vector3.zero;


        canvasTransform.DOScale(new Vector3(0.002f, 0.002f, 0.002f), 0.3f).SetEase(Ease.OutBack);


    }



    public void SetCurrentElement(string chemicalName)
{
    switch(chemicalName.Trim().ToUpper())
    {
        case "NACL":  currentElement = NaCl_Data; break;
        case "KCL":   currentElement = KCl_Data; break;
        case "LICL":  currentElement = LiCl_Data; break;
        case "CUCL2": currentElement = CuCl2_Data; break;
    }
    Debug.Log($"Current element set to: {(currentElement != null ? currentElement.sampleName : "NULL")}");
}

private void UpdateUIForCurrentChemical()
{
    if (uiController != null && currentElement != null)
    {
        uiController.Populate(currentElement);
    }
}

public void ExecuteBurnSample()
{
    if (liveLoopObject == null) return;

    GameObject activeBurner = GameObject.FindWithTag("Burner");
    if (activeBurner == null) return;

    burnButtonUI.SetActive(false);
    VisualEffect vfxFlame = activeBurner.GetComponentInChildren<VisualEffect>();
    AudioSource burnerAudio = activeBurner.GetComponent<AudioSource>();

    Transform flameAnchorTransform = activeBurner.transform.Find("FlameAnchor");
    Vector3 flamePosition = flameAnchorTransform != null ? flameAnchorTransform.position : activeBurner.transform.position + (Vector3.up * 0.22f);

    Sequence burnSequence = DOTween.Sequence();
    
    burnSequence.Append(liveLoopObject.transform.DOMove(flamePosition, 0.6f));
    burnSequence.Join(liveLoopObject.transform.DORotate(new Vector3(90f, liveLoopObject.transform.eulerAngles.y, liveLoopObject.transform.eulerAngles.z), 0.6f));

    burnSequence.AppendCallback(() =>
{
    Debug.Log("Burn sequence reached: Calling UpdateUI...");
    ApplyVFXFlameColor(vfxFlame, chemicalManager.SelectedChemical);
    
    // --- UPDATED: Trigger UI Display and Positioning ---
    // --- FULL VERSION: POSITIONING AND VISIBILITY ---
    GameObject activeBurner = GameObject.FindWithTag("Burner");

    if (activeBurner != null) 
    {
        // 1. Parent the UI to the burner so it tracks its movement
        uiController.transform.SetParent(activeBurner.transform);

        // 2. Set Local Position (Adjust these numbers to move the UI exactly where you want)
        // X: side-to-side, Y: height, Z: depth relative to the burner
        uiController.transform.localPosition = new Vector3(0.05f, 0.56f, 0f);
        // 3. Reset Rotation to default so it doesn't inherit tilted parent rotation
        uiController.transform.localRotation = Quaternion.identity;

        // 4. Force UI to face the player's camera (Billboard effect)
        // We modify the target so the UI stays level with the horizon
        Vector3 cameraPos = Camera.main.transform.position;
        cameraPos.y = uiController.transform.position.y; 
        uiController.transform.LookAt(cameraPos);
        uiController.transform.Rotate(0, 180, 0);

        // 5. Ensure the object is active in the hierarchy
        uiController.gameObject.SetActive(true);

        // 6. Force the UI Toolkit root to be visible
        var uiDoc = uiController.GetComponent<UIDocument>();
        if (uiDoc != null && uiDoc.rootVisualElement != null)
        {
            uiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
        }
    }
    else
    {
        Debug.LogWarning("Burner object not found! Ensure it has the tag 'Burner'.");
    }
    // ----------------------------------------------------

    UpdateUIForCurrentChemical();

    if (burnerAudio != null && chemicalSizzleSFX != null) burnerAudio.PlayOneShot(chemicalSizzleSFX);
    if (saltSampleOnLoop != null) saltSampleOnLoop.SetActive(false);
});

    burnSequence.AppendInterval(3.0f);
    burnSequence.Append(liveLoopObject.transform.DOMove(loopHomePosition, 0.8f));
    burnSequence.Join(liveLoopObject.transform.DORotateQuaternion(loopHomeRotation, 0.8f));

    burnSequence.AppendCallback(() => 
    {
        AudioSource loopAudio = liveLoopObject.GetComponent<AudioSource>();
        if (loopAudio != null && loopDipSFX != null) loopAudio.PlayOneShot(loopDipSFX);
    });

    burnSequence.OnComplete(() =>
    {
        ResetVFXFlameColor(vfxFlame);
        chemicalManager.SetUnknownSelected(false);
    });
}



    // (Include your existing MatchSampleColor, ApplyVFXFlameColor, and ResetVFXFlameColor methods below)


   private void MatchSampleColor(GameObject sampleObject, string chemical)


    {


        Renderer rend = sampleObject.GetComponent<Renderer>();


        if (rend == null) return;



        Color targetColor = Color.white;


        switch (chemical)


        {


            case "NaCl":  targetColor = Color.white; break;


            case "LiCl":  targetColor = Color.white; break;


            case "CuCl2": targetColor = new Color(0.15f, 0.65f, 0.55f); break;


            case "KCl":   targetColor = Color.white; break;


        }


        rend.sharedMaterial.color = targetColor;


    }



    private void ApplyVFXFlameColor(VisualEffect vfx, string chemical)


    {


        if (vfx == null)


        {


            Debug.LogError("ApplyVFXFlameColor aborted because passed 'vfx' parameter is null!");


            return;


        }



        if (string.IsNullOrEmpty(chemical))


        {


            Debug.LogError("ApplyVFXFlameColor aborted because 'chemical' string is null or empty!");


            return;


        }



        string cleanedChemical = chemical.Trim().ToUpper();


        Color targetColor = new Color(0.1f, 0.5f, 1.0f); // Default fall-back base blue


        float intensity = 1.0f;


       


        switch (cleanedChemical)


        {


            // NaCl: Pure sodium yellow-orange.


            case "NACL":  


                targetColor = new Color(1.0f, 0.35f, 0.0f);


                intensity = 3.0f;


                break;



            // LICL: Deep Crimson Red.


            // We keep Blue at ABSOLUTE ZERO (0.0f) to stop it from turning hot pink/magenta.


            case "LICL":  


                targetColor = new Color(1.0f, 0.0f, 0.0f);  


                intensity = 3.0f; // Kept moderate to ensure it stays a deep blood-red


                break;



            // CUCL2: Copper green/teal.


            case "CUCL2":


                targetColor = new Color(0.0f, 1.0f, 0.4f);


                intensity = 3.0f;


                break;



            // KCL: Authentic Potassium Lilac / Faint Violet.


            // We drop the intensity significantly down to 1.2f so it doesn't blow out into neon pink.


            // We add a tiny splash of red and keep blue dominant to capture the gentle lilac hue.


            case "KCL":  


                targetColor = new Color(0.35f, 0.15f, 0.8f);  


                intensity = 1.2f; // Low intensity prevents the additive shader from bleeding into hot pink


                break;



            default:      


                Debug.LogWarning($"MATCH FAILED: '{cleanedChemical}' did not match any flame color cases!");


                break;


        }



        Debug.Log($"SUCCESS: Applying color values to VFX property '{vfxColorPropertyName}'. RGB: ({targetColor.r * intensity}, {targetColor.g * intensity}, {targetColor.b * intensity})");



        // Construct the clean HDR vector, locking Alpha strictly at 1.0f to prevent blowout artifacts


        Vector4 finalHdrColor = new Vector4(targetColor.r * intensity, targetColor.g * intensity, targetColor.b * intensity, 1.0f);


        vfx.SetVector4(vfxColorPropertyName, finalHdrColor);


    }



    private void ResetVFXFlameColor(VisualEffect vfx)


    {


        if (vfx == null) return;


       


        // This completely wipes out the script's property override


        // and safely forces the VFX Graph back to its default asset color.


        vfx.ResetOverride(vfxColorPropertyName);


    }

    public void OnFlameTestComplete()

    {

        // This line triggers the UI to appear with the specific data

        uiController.Populate(currentElement);

    }


} 