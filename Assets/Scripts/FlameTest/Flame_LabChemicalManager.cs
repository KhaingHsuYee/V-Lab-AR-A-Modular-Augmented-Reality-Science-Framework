 using UnityEngine;

using DG.Tweening;


public class Flame_LabChemicalManager : MonoBehaviour

{

    [Header("UI Reference Elements")]

    public GameObject saltMenuUIDocument;

    public GameObject pickButtonUI;


    [Header("Prefabs")]

    public GameObject prefabNaCl;

    public GameObject prefabLiCl;

    public GameObject prefabCuCl2;

    public GameObject prefabKCl;

    public GameObject prefabBeaker;


    [Header("Audio")]

    public AudioClip dishDropSoundClip;


    private string selectedChemical;

    private GameObject currentSpawnedDish;

    private GameObject currentSpawnedLoop;

    private GameObject currentSpawnedBeaker;

    private bool isUnknownSelected;

    private string[] chemicalList = { "NaCl", "LiCl", "CuCl2", "KCl" };


    public void SelectSalt(string chemicalName)
{
    GameObject activeBurner = GameObject.FindWithTag("Burner");
    if (activeBurner == null) return;

    ResetLabSession(); 

    selectedChemical = chemicalName;

    // --- Bridge to LabFlameTester ---
    LabFlameTester flameTester = GetComponent<LabFlameTester>();
    if (flameTester != null)
    {
        flameTester.SetCurrentElement(chemicalName);
    }
    // --------------------------------

    Vector3 cameraToBurner = activeBurner.transform.position - Camera.main.transform.position;
    cameraToBurner.y = 0;
    cameraToBurner.Normalize();
    Vector3 viewRelativeRight = Quaternion.Euler(0, 90, 0) * cameraToBurner;

    Vector3 dishTargetPos = activeBurner.transform.position + (viewRelativeRight * 0.15f) - (cameraToBurner * 0.03f);
    Vector3 beakerTargetPos = activeBurner.transform.position - (viewRelativeRight * 0.15f) - (cameraToBurner * 0.03f);

    GameObject targetPrefab = GetPrefabByName(chemicalName);
    currentSpawnedDish = Instantiate(targetPrefab, dishTargetPos + Vector3.up * 1.0f, activeBurner.transform.rotation);
    currentSpawnedDish.transform.DOMove(dishTargetPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() => PlayDropSound(currentSpawnedDish));

    currentSpawnedBeaker = Instantiate(prefabBeaker, beakerTargetPos + Vector3.up * 1.0f, activeBurner.transform.rotation);
    currentSpawnedBeaker.transform.DOMove(beakerTargetPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() => PlayDropSound(currentSpawnedBeaker));
    
    currentSpawnedLoop = currentSpawnedBeaker.transform.Find("MetalLoop").gameObject; 

    if (flameTester != null) flameTester.InitializeSpawnedLoop(currentSpawnedLoop, beakerTargetPos);

    if (saltMenuUIDocument != null) saltMenuUIDocument.SetActive(false);
    pickButtonUI.SetActive(true);
}


private void PlayDropSound(GameObject target)

{

    AudioSource source = target.GetComponent<AudioSource>();

    if (source != null && dishDropSoundClip != null)

    {

        source.PlayOneShot(dishDropSoundClip);

    }

}

    // New cleanup method to be called when you double-tap the dish

    public void ResetLabSession()

    {

        if (currentSpawnedDish != null) Destroy(currentSpawnedDish);

        if (currentSpawnedBeaker != null) Destroy(currentSpawnedBeaker);

        currentSpawnedDish = null;

        currentSpawnedBeaker = null;

        currentSpawnedLoop = null;

        // Add this to hide the UI when the session resets
    LabFlameTester tester = GetComponent<LabFlameTester>();
    if (tester != null && tester.uiController != null)
    {
        tester.uiController.gameObject.SetActive(false);
    }

    }

   

    public void SelectUnknownSalt()


    {


        int randomIndex = Random.Range(0, chemicalList.Length);


        isUnknownSelected = true;


        SelectSalt(chemicalList[randomIndex]);


    }



    private GameObject GetPrefabByName(string name)

    {

        switch (name)

        {

            case "NaCl": return prefabNaCl;

            case "LiCl": return prefabLiCl;

            case "CuCl2": return prefabCuCl2;

            case "KCl": return prefabKCl;

            default: return null;

        }

    }

    public string SelectedChemical => selectedChemical;

    public GameObject CurrentSpawnedDish => currentSpawnedDish;

    public bool IsUnknownSelected => isUnknownSelected;

    public void SetUnknownSelected(bool state) => isUnknownSelected = state;

} 