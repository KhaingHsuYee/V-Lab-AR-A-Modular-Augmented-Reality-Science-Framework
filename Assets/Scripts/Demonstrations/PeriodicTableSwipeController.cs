using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PeriodicTableSwipeController : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Assign your 8 audio clips here matching the pages (0 = Legend, 1 = Alkali, etc.)")]
    public AudioClip[] pageAudioClips = new AudioClip[8];
    
    private AudioSource audioSource;
    private UIDocument uiDocument;
    private VisualElement slidingContainer;
    
    private Vector2 touchStartPos;
    private int currentPage = 0;
    private readonly int totalPages = 8; 

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        if (root == null) return;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Tell the UI to ignore Unity's standard click events so our raycast works
        root.Query<VisualElement>().ForEach(element => element.pickingMode = PickingMode.Ignore);

        slidingContainer = root.Q<VisualElement>("sliding-container");
    }

    public void ToggleAudioForPage(int index)
    {
        if (audioSource == null) return;

        if (index >= 0 && index < pageAudioClips.Length && pageAudioClips[index] != null)
        {
            // If the audio is currently playing THIS specific page's clip, turn it off.
            if (audioSource.isPlaying && audioSource.clip == pageAudioClips[index])
            {
                Debug.Log("Stopping sound for page " + index);
                audioSource.Stop();
            }
            // Otherwise, play the clip for this page.
            else
            {
                Debug.Log("Playing sound for page " + index);
                audioSource.Stop(); // Ensure any other sounds are stopped first
                audioSource.clip = pageAudioClips[index];
                audioSource.Play();
            }
        }
    }

    private void Update()
    {
        Vector2 currentPos = Vector2.zero;
        bool wasPressedThisFrame = false;
        bool wasReleasedThisFrame = false;

        // Read input from Mobile Touchscreen OR PC Simulator Mouse
        if (Touchscreen.current != null)
        {
            currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            wasPressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            wasReleasedThisFrame = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        }
        else if (Mouse.current != null)
        {
            currentPos = Mouse.current.position.ReadValue();
            wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
        }

        if (wasPressedThisFrame)
        {
            touchStartPos = currentPos; 
        }
        else if (wasReleasedThisFrame)
        {
            float swipeDistance = currentPos.x - touchStartPos.x;
            
            // SWIPE RIGHT (Previous Page)
            if (swipeDistance > 50f && currentPage > 0) 
            {
                ChangePage(currentPage - 1);
            }
            // SWIPE LEFT (Next Page)
            else if (swipeDistance < -50f && currentPage < totalPages - 1) 
            {
                ChangePage(currentPage + 1);
            }
            // TAP DETECTED (Toggle Audio)
            else if (Mathf.Abs(swipeDistance) < 10f) 
            {
                Ray ray = Camera.main.ScreenPointToRay(currentPos);
                
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform == this.transform)
                    {
                        ToggleAudioForPage(currentPage);
                    }
                }
            }
        }
    }

    private void ChangePage(int targetPage)
    {
        currentPage = targetPage;
        
        // Always stop the audio when the user swipes to a new page
        if (audioSource != null) audioSource.Stop();
        
        float translationValue = currentPage * -12.5f; 
        if (slidingContainer != null)
        {
            slidingContainer.style.translate = new StyleTranslate(new Translate(new Length(translationValue, LengthUnit.Percent), 0, 0));
        }
    }
}