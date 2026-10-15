using UnityEngine;

[CreateAssetMenu(fileName = "NewElement", menuName = "Chemistry/Element Data")]
public class ElementData_Periodic : ScriptableObject
{
    [Header("Core Info")]
    public string elementName = "Hydrogen";
    public string symbol = "H";
    public int atomicNumber = 1;
    public float atomicWeight = 1.008f;
    public string category = "Reactive Nonmetal";
    
    [Header("Atomic Structure")]
    public int protons = 1;
    public int neutrons = 0;
    public int[] electronShells = { 1 };
    public string electronConfiguration = "1s1";
    public float electronegativity = 2.20f;
    public float atomicRadius = 53f; // in picometers
    public float ionizationEnergy = 13.598f; // in eV

    [Header("Physical Properties")]
    public string phaseAtSTP = "Gas";
    public float density = 0.00008988f; // g/cm3
    public float meltingPoint = 13.99f; // Kelvin
    public float boilingPoint = 20.271f; // Kelvin

    [Header("History & Description")]
    public string discoveredBy = "Henry Cavendish (1766)";
    [TextArea(5, 10)]
    public string description = "Hydrogen is the chemical element with the symbol H and atomic number 1. It is the lightest element in the periodic table.";
}