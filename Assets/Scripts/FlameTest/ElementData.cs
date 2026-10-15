using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "FlameTest/ElementData")]
public class ElementData : ScriptableObject
{
    [Header("Sample Identification")]
    public string sampleName;    // "Sodium Chloride"
    public string formula;       // "NaCl"
    
    [Header("Atomic Analysis (Active Cation)")]
    public string metalName;     // "Sodium"
    public int protons;          // 11
    public int neutrons;         // 12
    
    [Header("Spectral Physics")]
    public string wavelength;    // "589.0 nm"
    public string photonEnergy;  // "2.10 eV"
    public string transition;    // "3p -> 3s"
    public float[] spectralData; 
}