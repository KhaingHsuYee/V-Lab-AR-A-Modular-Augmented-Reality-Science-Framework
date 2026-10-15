using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class SolarSystemDashboard : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioClip[] planetAudioClips = new AudioClip[11];
    
    private AudioSource audioSource;
    private UIDocument uiDocument;
    private VisualElement root;

    private int currentPlanetIndex = -1; 
    private Vector2 touchStartPos;

    private Label txtName, txtType, txtGravity, txtMass, txtEscape, txtOrbit;

    private struct AstroPhysicsData
    {
        public string Name, Type, Gravity, Mass, EscapeVelocity, OrbitalVelocity;
        public AstroPhysicsData(string name, string type, string grav, string mass, string esc, string orb)
        {
            Name = name; Type = type; Gravity = grav; Mass = mass; EscapeVelocity = esc; OrbitalVelocity = orb;
        }
    }

    private readonly AstroPhysicsData[] database = new AstroPhysicsData[]
    {
        new AstroPhysicsData("Sun", "Yellow Dwarf Star (G2V)", "274.0 m/s²", "1,989,000 x 10²⁴ kg", "617.7 km/s", "220.0 km/s (Galactic)"), 
        new AstroPhysicsData("Mercury", "Terrestrial Planet", "3.7 m/s²", "0.330 x 10²⁴ kg", "4.3 km/s", "47.4 km/s"), 
        new AstroPhysicsData("Venus", "Terrestrial Planet", "8.87 m/s²", "4.87 x 10²⁴ kg", "10.4 km/s", "35.0 km/s"), 
        new AstroPhysicsData("Earth", "Terrestrial Planet", "9.8 m/s²", "5.97 x 10²⁴ kg", "11.2 km/s", "29.8 km/s"), 
        new AstroPhysicsData("Mars", "Terrestrial Planet", "3.71 m/s²", "0.642 x 10²⁴ kg", "5.0 km/s", "24.1 km/s"), 
        new AstroPhysicsData("Jupiter", "Gas Giant", "24.79 m/s²", "1,898 x 10²⁴ kg", "59.5 km/s", "13.1 km/s"), 
        new AstroPhysicsData("Saturn", "Gas Giant", "10.44 m/s²", "568 x 10²⁴ kg", "35.5 km/s", "9.7 km/s"), 
        new AstroPhysicsData("Uranus", "Ice Giant", "8.69 m/s²", "86.8 x 10²⁴ kg", "21.3 km/s", "6.8 km/s"), 
        new AstroPhysicsData("Neptune", "Ice Giant", "11.15 m/s²", "102 x 10²⁴ kg", "23.5 km/s", "5.4 km/s"), 
        new AstroPhysicsData("Pluto", "Dwarf Planet", "0.62 m/s²", "0.0146 x 10²⁴ kg", "1.3 km/s", "4.7 km/s"), 
        new AstroPhysicsData("Moon", "Natural Satellite", "1.62 m/s²", "0.073 x 10²⁴ kg", "2.38 km/s", "1.02 km/s") 
    };

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // This stops UI Toolkit from blocking background raycasts
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
        }
    }

    private void Update()
    {
        Vector2 currentPos = Vector2.zero;
        bool wasPressed = false;
        bool wasReleased = false;

        // 1. Read input safely
        if (Touchscreen.current != null)
        {
            currentPos = Touchscreen.current.primaryTouch.position.ReadValue();
            wasPressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            wasReleased = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        }
        else if (Mouse.current != null)
        {
            currentPos = Mouse.current.position.ReadValue();
            wasPressed = Mouse.current.leftButton.wasPressedThisFrame;
            wasReleased = Mouse.current.leftButton.wasReleasedThisFrame;
        }

        // 2. Track the start of the tap
        if (wasPressed) 
        {
            touchStartPos = currentPos; 
        }
        // 3. On release, check if it was a quick tap (not a long swipe to rotate)
        else if (wasReleased)
        {
            float distance = Vector2.Distance(touchStartPos, currentPos);
            if (distance < 15f) 
            {
                CheckForDashboardTap(currentPos);
            }
        }
    }

    private void CheckForDashboardTap(Vector2 screenPos)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        
        // Shoot a laser. ONLY toggle audio if the laser hits THIS specific dashboard object.
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == this.transform)
            {
                ToggleAudio();
            }
        }
    }

    private void ToggleAudio()
    {
        if (audioSource == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop(); 
        }
        else if (currentPlanetIndex >= 0 && currentPlanetIndex < planetAudioClips.Length && planetAudioClips[currentPlanetIndex] != null)
        {
            audioSource.clip = planetAudioClips[currentPlanetIndex];
            audioSource.Play();
        }
    }

    public void LoadPlanetData(int index)
    {
        currentPlanetIndex = index; 

        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        root = uiDocument.rootVisualElement;

        txtName = root.Q<Label>("txt-planet-name");
        txtType = root.Q<Label>("txt-planet-type");
        txtGravity = root.Q<Label>("txt-gravity");
        txtMass = root.Q<Label>("txt-mass");
        txtEscape = root.Q<Label>("txt-escape");
        txtOrbit = root.Q<Label>("txt-orbit");

        if (index >= 0 && index < database.Length)
        {
            AstroPhysicsData data = database[index];
            if (txtName != null) txtName.text = data.Name;
            if (txtType != null) txtType.text = data.Type;
            if (txtGravity != null) txtGravity.text = data.Gravity;
            if (txtMass != null) txtMass.text = data.Mass;
            if (txtEscape != null) txtEscape.text = data.EscapeVelocity;
            if (txtOrbit != null) txtOrbit.text = data.OrbitalVelocity;
        }

        if (index >= 0 && index < planetAudioClips.Length && planetAudioClips[index] != null)
        {
            audioSource.Stop();
            audioSource.clip = planetAudioClips[index];
            audioSource.Play();
        }
    }
}