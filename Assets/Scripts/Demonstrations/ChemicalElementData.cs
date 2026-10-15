using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChemicalElementData
{
    public string Name;
    public string Symbol;
    public int AtomicNumber;
    public string Group;
    public string Fact;
    public string History;
    public string Appearance;
    public string StateOfMatter;

    public ChemicalElementData(string name, string symbol, int atomicNumber, string group, string fact, string history, string appearance, string stateOfMatter)
    {
        Name = name;
        Symbol = symbol;
        AtomicNumber = atomicNumber;
        Group = group;
        Fact = fact;
        History = history;
        Appearance = appearance;
        StateOfMatter = stateOfMatter;
    }
}

public static class ElementDatabase
{
    public static Dictionary<string, ChemicalElementData> Elements = new Dictionary<string, ChemicalElementData>()
    {
        // Period 1
        {"H", new ChemicalElementData("Hydrogen", "H", 1, "Nonmetal", "Most abundant element in the universe.", "Recognized in 1766 by Cavendish.", "Colorless", "Gas")},
        {"He", new ChemicalElementData("Helium", "He", 2, "Noble Gas", "Second most abundant element.", "Discovered in solar spectrum in 1868.", "Colorless", "Gas")},

        // Period 2
        {"Li", new ChemicalElementData("Lithium", "Li", 3, "Alkali Metal", "Lightest metal.", "Discovered in 1817.", "Silvery-white", "Solid")},
        {"Be", new ChemicalElementData("Beryllium", "Be", 4, "Alkaline Earth", "Used in aerospace components.", "Discovered in 1798.", "Lead-gray", "Solid")},
        {"B", new ChemicalElementData("Boron", "B", 5, "Metalloid", "Used in fiberglass and detergents.", "Isolated in 1808.", "Black-brown", "Solid")},
        {"C", new ChemicalElementData("Carbon", "C", 6, "Nonmetal", "Basis of all known organic life.", "Known since ancient times.", "Black/Transparent", "Solid")},
        {"N", new ChemicalElementData("Nitrogen", "N", 7, "Nonmetal", "Makes up 78% of Earth's atmosphere.", "Discovered in 1772.", "Colorless", "Gas")},
        {"O", new ChemicalElementData("Oxygen", "O", 8, "Nonmetal", "Essential for respiration.", "Discovered in 1774.", "Colorless", "Gas")},
        {"F", new ChemicalElementData("Fluorine", "F", 9, "Halogen", "Most electronegative element.", "Isolated in 1886.", "Pale yellow", "Gas")},
        {"Ne", new ChemicalElementData("Neon", "Ne", 10, "Noble Gas", "Glows red-orange in discharge tubes.", "Discovered in 1898.", "Colorless", "Gas")},

        // Period 3
        {"Na", new ChemicalElementData("Sodium", "Na", 11, "Alkali Metal", "Highly reactive, stored in oil.", "Isolated in 1807.", "Silvery-white", "Solid")},
        {"Mg", new ChemicalElementData("Magnesium", "Mg", 12, "Alkaline Earth", "Burns with a brilliant white light.", "Isolated in 1808.", "Shiny grey", "Solid")},
        {"Al", new ChemicalElementData("Aluminum", "Al", 13, "Post-Transition", "Most abundant metal in Earth's crust.", "Isolated in 1825.", "Silvery-gray", "Solid")},
        {"Si", new ChemicalElementData("Silicon", "Si", 14, "Metalloid", "Crucial for semiconductors.", "Isolated in 1824.", "Blue-grey metallic", "Solid")},
        {"P", new ChemicalElementData("Phosphorus", "P", 15, "Nonmetal", "Used in fertilizers and matches.", "Discovered in 1669.", "White/Red/Black", "Solid")},
        {"S", new ChemicalElementData("Sulfur", "S", 16, "Nonmetal", "Has a distinct smell of rotten eggs.", "Known since ancient times.", "Lemon yellow", "Solid")},
        {"Cl", new ChemicalElementData("Chlorine", "Cl", 17, "Halogen", "Used widely as a disinfectant.", "Discovered in 1774.", "Pale green", "Gas")},
        {"Ar", new ChemicalElementData("Argon", "Ar", 18, "Noble Gas", "Used in incandescent light bulbs.", "Discovered in 1894.", "Colorless", "Gas")},

        // Period 4
        {"K", new ChemicalElementData("Potassium", "K", 19, "Alkali Metal", "Essential for nerve function.", "Isolated in 1807.", "Silvery-white", "Solid")},
        {"Ca", new ChemicalElementData("Calcium", "Ca", 20, "Alkaline Earth", "Essential for bone structure.", "Isolated in 1808.", "Silvery-white", "Solid")},
        {"Sc", new ChemicalElementData("Scandium", "Sc", 21, "Transition Metal", "Used in aerospace alloys.", "Discovered in 1879.", "Silvery-white", "Solid")},
        {"Ti", new ChemicalElementData("Titanium", "Ti", 22, "Transition Metal", "High strength-to-weight ratio.", "Discovered in 1791.", "Silvery-grey", "Solid")},
        {"V", new ChemicalElementData("Vanadium", "V", 23, "Transition Metal", "Used to strengthen steel.", "Discovered in 1801.", "Blue-silver", "Solid")},
        {"Cr", new ChemicalElementData("Chromium", "Cr", 24, "Transition Metal", "Provides corrosion resistance.", "Discovered in 1797.", "Silvery metallic", "Solid")},
        {"Mn", new ChemicalElementData("Manganese", "Mn", 25, "Transition Metal", "Crucial for steel production.", "Discovered in 1774.", "Silvery metallic", "Solid")},
        {"Fe", new ChemicalElementData("Iron", "Fe", 26, "Transition Metal", "Most common element on Earth by mass.", "Known since ancient times.", "Lustrous metallic", "Solid")},
        {"Co", new ChemicalElementData("Cobalt", "Co", 27, "Transition Metal", "Used in lithium-ion batteries.", "Discovered in 1735.", "Metallic gray", "Solid")},
        {"Ni", new ChemicalElementData("Nickel", "Ni", 28, "Transition Metal", "Resists corrosion even at high temps.", "Discovered in 1751.", "Lustrous, silvery", "Solid")},
        {"Cu", new ChemicalElementData("Copper", "Cu", 29, "Transition Metal", "Excellent electrical conductor.", "Known since ancient times.", "Red-orange", "Solid")},
        {"Zn", new ChemicalElementData("Zinc", "Zn", 30, "Transition Metal", "Used to galvanize other metals.", "Known since ancient times.", "Silver-gray", "Solid")},
        {"Ga", new ChemicalElementData("Gallium", "Ga", 31, "Post-Transition", "Melts in the palm of your hand.", "Discovered in 1875.", "Silvery-blue", "Solid")},
        {"Ge", new ChemicalElementData("Germanium", "Ge", 32, "Metalloid", "Used in fiber optics.", "Discovered in 1886.", "Grayish-white", "Solid")},
        {"As", new ChemicalElementData("Arsenic", "As", 33, "Metalloid", "Notoriously toxic.", "Known since ancient times.", "Metallic grey", "Solid")},
        {"Se", new ChemicalElementData("Selenium", "Se", 34, "Nonmetal", "Photovoltaic properties.", "Discovered in 1817.", "Black/Red/Grey", "Solid")},
        {"Br", new ChemicalElementData("Bromine", "Br", 35, "Halogen", "One of two elements liquid at room temp.", "Discovered in 1826.", "Red-brown", "Liquid")},
        {"Kr", new ChemicalElementData("Krypton", "Kr", 36, "Noble Gas", "Used in photography flashes.", "Discovered in 1898.", "Colorless", "Gas")},

        // Period 5
        {"Rb", new ChemicalElementData("Rubidium", "Rb", 37, "Alkali Metal", "Ignites spontaneously in air.", "Discovered in 1861.", "Silvery-white", "Solid")},
        {"Sr", new ChemicalElementData("Strontium", "Sr", 38, "Alkaline Earth", "Burns with a bright red flame.", "Discovered in 1790.", "Silvery-white", "Solid")},
        {"Y", new ChemicalElementData("Yttrium", "Y", 39, "Transition Metal", "Used in LEDs and phosphors.", "Discovered in 1794.", "Silvery-white", "Solid")},
        {"Zr", new ChemicalElementData("Zirconium", "Zr", 40, "Transition Metal", "Highly resistant to corrosion.", "Discovered in 1789.", "Silvery-white", "Solid")},
        {"Nb", new ChemicalElementData("Niobium", "Nb", 41, "Transition Metal", "Used in superconducting magnets.", "Discovered in 1801.", "Gray metallic", "Solid")},
        {"Mo", new ChemicalElementData("Molybdenum", "Mo", 42, "Transition Metal", "Extremely high melting point.", "Discovered in 1778.", "Gray metallic", "Solid")},
        {"Tc", new ChemicalElementData("Technetium", "Tc", 43, "Transition Metal", "First artificially produced element.", "Discovered in 1937.", "Silvery-grey", "Solid")},
        {"Ru", new ChemicalElementData("Ruthenium", "Ru", 44, "Transition Metal", "Used in wear-resistant electrical contacts.", "Discovered in 1844.", "Silvery-white", "Solid")},
        {"Rh", new ChemicalElementData("Rhodium", "Rh", 45, "Transition Metal", "One of the rarest and most valuable metals.", "Discovered in 1803.", "Silvery-white", "Solid")},
        {"Pd", new ChemicalElementData("Palladium", "Pd", 46, "Transition Metal", "Used in catalytic converters.", "Discovered in 1803.", "Silvery-white", "Solid")},
        {"Ag", new ChemicalElementData("Silver", "Ag", 47, "Transition Metal", "Highest electrical conductivity of any element.", "Known since ancient times.", "Silvery-white", "Solid")},
        {"Cd", new ChemicalElementData("Cadmium", "Cd", 48, "Transition Metal", "Toxic metal used in older batteries.", "Discovered in 1817.", "Silvery-bluish", "Solid")},
        {"In", new ChemicalElementData("Indium", "In", 49, "Post-Transition", "Used in touch screens (ITO).", "Discovered in 1863.", "Silvery-white", "Solid")},
        {"Sn", new ChemicalElementData("Tin", "Sn", 50, "Post-Transition", "Used as a protective coating for steel.", "Known since ancient times.", "Silvery-white", "Solid")},
        {"Sb", new ChemicalElementData("Antimony", "Sb", 51, "Metalloid", "Used in flame retardants.", "Known since ancient times.", "Silvery-grey", "Solid")},
        {"Te", new ChemicalElementData("Tellurium", "Te", 52, "Metalloid", "Used in solar panels.", "Discovered in 1782.", "Silvery-grey", "Solid")},
        {"I", new ChemicalElementData("Iodine", "I", 53, "Halogen", "Sublimes into a purple gas.", "Discovered in 1811.", "Violet-dark grey", "Solid")},
        {"Xe", new ChemicalElementData("Xenon", "Xe", 54, "Noble Gas", "Used in ion propulsion engines.", "Discovered in 1898.", "Colorless", "Gas")},

        // Period 6 (Including Lanthanides)
        {"Cs", new ChemicalElementData("Cesium", "Cs", 55, "Alkali Metal", "Used in atomic clocks.", "Discovered in 1860.", "Silvery-gold", "Solid")},
        {"Ba", new ChemicalElementData("Barium", "Ba", 56, "Alkaline Earth", "Used in X-ray imaging.", "Discovered in 1808.", "Silvery-grey", "Solid")},
        
        // Lanthanide Series
        {"La", new ChemicalElementData("Lanthanum", "La", 57, "Lanthanide", "Used in camera lenses.", "Discovered in 1839.", "Silvery-white", "Solid")},
        {"Ce", new ChemicalElementData("Cerium", "Ce", 58, "Lanthanide", "Sparks when struck.", "Discovered in 1803.", "Silvery-white", "Solid")},
        {"Pr", new ChemicalElementData("Praseodymium", "Pr", 59, "Lanthanide", "Used in aircraft engines.", "Discovered in 1885.", "Silvery-white", "Solid")},
        {"Nd", new ChemicalElementData("Neodymium", "Nd", 60, "Lanthanide", "Creates powerful permanent magnets.", "Discovered in 1885.", "Silvery-white", "Solid")},
        {"Pm", new ChemicalElementData("Promethium", "Pm", 61, "Lanthanide", "Radioactive and luminous.", "Discovered in 1945.", "Metallic", "Solid")},
        {"Sm", new ChemicalElementData("Samarium", "Sm", 62, "Lanthanide", "Used in cancer treatments.", "Discovered in 1879.", "Silvery-white", "Solid")},
        {"Eu", new ChemicalElementData("Europium", "Eu", 63, "Lanthanide", "Used in anti-forgery marks on Euros.", "Discovered in 1901.", "Silvery-white", "Solid")},
        {"Gd", new ChemicalElementData("Gadolinium", "Gd", 64, "Lanthanide", "Used in MRI contrast agents.", "Discovered in 1880.", "Silvery-white", "Solid")},
        {"Tb", new ChemicalElementData("Terbium", "Tb", 65, "Lanthanide", "Used in green phosphors.", "Discovered in 1843.", "Silvery-white", "Solid")},
        {"Dy", new ChemicalElementData("Dysprosium", "Dy", 66, "Lanthanide", "Highly susceptible to magnetization.", "Discovered in 1886.", "Silvery-white", "Solid")},
        {"Ho", new ChemicalElementData("Holmium", "Ho", 67, "Lanthanide", "Highest magnetic strength of any element.", "Discovered in 1878.", "Silvery-white", "Solid")},
        {"Er", new ChemicalElementData("Erbium", "Er", 68, "Lanthanide", "Used in fiber optic amplifiers.", "Discovered in 1843.", "Silvery-white", "Solid")},
        {"Tm", new ChemicalElementData("Thulium", "Tm", 69, "Lanthanide", "Used in portable X-ray devices.", "Discovered in 1879.", "Silvery-grey", "Solid")},
        {"Yb", new ChemicalElementData("Ytterbium", "Yb", 70, "Lanthanide", "Used in atomic clocks.", "Discovered in 1878.", "Silvery-white", "Solid")},
        {"Lu", new ChemicalElementData("Lutetium", "Lu", 71, "Lanthanide", "Most expensive rare earth element.", "Discovered in 1907.", "Silvery-white", "Solid")},

        // Continuing Period 6
        {"Hf", new ChemicalElementData("Hafnium", "Hf", 72, "Transition Metal", "Used in nuclear control rods.", "Discovered in 1923.", "Steel-grey", "Solid")},
        {"Ta", new ChemicalElementData("Tantalum", "Ta", 73, "Transition Metal", "Used in cell phone capacitors.", "Discovered in 1802.", "Grey-blue", "Solid")},
        {"W", new ChemicalElementData("Tungsten", "W", 74, "Transition Metal", "Highest melting point of any metal.", "Discovered in 1781.", "Greyish-white", "Solid")},
        {"Re", new ChemicalElementData("Rhenium", "Re", 75, "Transition Metal", "Used in jet engine exhaust nozzles.", "Discovered in 1925.", "Silvery-grey", "Solid")},
        {"Os", new ChemicalElementData("Osmium", "Os", 76, "Transition Metal", "Densest naturally occurring element.", "Discovered in 1803.", "Bluish-white", "Solid")},
        {"Ir", new ChemicalElementData("Iridium", "Ir", 77, "Transition Metal", "Most corrosion-resistant metal known.", "Discovered in 1803.", "Silvery-white", "Solid")},
        {"Pt", new ChemicalElementData("Platinum", "Pt", 78, "Transition Metal", "Highly valuable catalytic metal.", "Discovered in 1735.", "Silvery-white", "Solid")},
        {"Au", new ChemicalElementData("Gold", "Au", 79, "Transition Metal", "Highly malleable and non-reactive.", "Known since ancient times.", "Metallic yellow", "Solid")},
        {"Hg", new ChemicalElementData("Mercury", "Hg", 80, "Transition Metal", "The only metal liquid at room temperature.", "Known since ancient times.", "Silvery liquid", "Liquid")},
        {"Tl", new ChemicalElementData("Thallium", "Tl", 81, "Post-Transition", "Highly toxic, formerly used as rat poison.", "Discovered in 1861.", "Silvery-white", "Solid")},
        {"Pb", new ChemicalElementData("Lead", "Pb", 82, "Post-Transition", "Dense, toxic, used in radiation shielding.", "Known since ancient times.", "Metallic grey", "Solid")},
        {"Bi", new ChemicalElementData("Bismuth", "Bi", 83, "Post-Transition", "Diamagnetic and expands upon freezing.", "Known since ancient times.", "Silvery-pink", "Solid")},
        {"Po", new ChemicalElementData("Polonium", "Po", 84, "Post-Transition", "Highly radioactive.", "Discovered in 1898 by Marie Curie.", "Silvery", "Solid")},
        {"At", new ChemicalElementData("Astatine", "At", 85, "Halogen", "Rarest naturally occurring element.", "Discovered in 1940.", "Black metallic (predicted)", "Solid")},
        {"Rn", new ChemicalElementData("Radon", "Rn", 86, "Noble Gas", "Radioactive gas that can accumulate in basements.", "Discovered in 1899.", "Colorless", "Gas")},

        // Period 7 (Including Actinides)
        {"Fr", new ChemicalElementData("Francium", "Fr", 87, "Alkali Metal", "Extremely radioactive and rare.", "Discovered in 1939.", "Metallic (predicted)", "Solid")},
        {"Ra", new ChemicalElementData("Radium", "Ra", 88, "Alkaline Earth", "Glows in the dark, formerly used in watches.", "Discovered in 1898 by Marie Curie.", "Silvery-white", "Solid")},
        
        // Actinide Series
        {"Ac", new ChemicalElementData("Actinium", "Ac", 89, "Actinide", "Glows blue due to radioactivity.", "Discovered in 1899.", "Silvery", "Solid")},
        {"Th", new ChemicalElementData("Thorium", "Th", 90, "Actinide", "Potential nuclear fuel alternative.", "Discovered in 1829.", "Silvery", "Solid")},
        {"Pa", new ChemicalElementData("Protactinium", "Pa", 91, "Actinide", "Highly toxic and radioactive.", "Discovered in 1913.", "Silvery-white", "Solid")},
        {"U", new ChemicalElementData("Uranium", "U", 92, "Actinide", "Primary fuel for nuclear reactors.", "Discovered in 1789.", "Silvery-grey", "Solid")},
        {"Np", new ChemicalElementData("Neptunium", "Np", 93, "Actinide", "Used in neutron detectors.", "Discovered in 1940.", "Silvery metallic", "Solid")},
        {"Pu", new ChemicalElementData("Plutonium", "Pu", 94, "Actinide", "Used in space probe power sources.", "Discovered in 1940.", "Silvery-white", "Solid")},
        {"Am", new ChemicalElementData("Americium", "Am", 95, "Actinide", "Used in smoke detectors.", "Discovered in 1944.", "Silvery-white", "Solid")},
        {"Cm", new ChemicalElementData("Curium", "Cm", 96, "Actinide", "Glows purple in the dark.", "Discovered in 1944.", "Silvery", "Solid")},
        {"Bk", new ChemicalElementData("Berkelium", "Bk", 97, "Actinide", "Synthetic and highly radioactive.", "Discovered in 1949.", "Silvery", "Solid")},
        {"Cf", new ChemicalElementData("Californium", "Cf", 98, "Actinide", "Used to start nuclear reactors.", "Discovered in 1950.", "Silvery", "Solid")},
        {"Es", new ChemicalElementData("Einsteinium", "Es", 99, "Actinide", "First discovered in nuclear fallout.", "Discovered in 1952.", "Silvery", "Solid")},
        {"Fm", new ChemicalElementData("Fermium", "Fm", 100, "Actinide", "Heaviest element formed by neutron bombardment.", "Discovered in 1952.", "Unknown", "Solid")},
        {"Md", new ChemicalElementData("Mendelevium", "Md", 101, "Actinide", "Named after the creator of the periodic table.", "Discovered in 1955.", "Unknown", "Solid")},
        {"No", new ChemicalElementData("Nobelium", "No", 102, "Actinide", "Synthetic radioactive metal.", "Discovered in 1958.", "Unknown", "Solid")},
        {"Lr", new ChemicalElementData("Lawrencium", "Lr", 103, "Actinide", "Synthetic radioactive metal.", "Discovered in 1961.", "Unknown", "Solid")},

        // Continuing Period 7 (Superheavy Synthetic Elements)
        {"Rf", new ChemicalElementData("Rutherfordium", "Rf", 104, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1964.", "Unknown", "Solid")},
        {"Db", new ChemicalElementData("Dubnium", "Db", 105, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1968.", "Unknown", "Solid")},
        {"Sg", new ChemicalElementData("Seaborgium", "Sg", 106, "Transition Metal", "Named after living chemist Glenn Seaborg.", "Discovered in 1974.", "Unknown", "Solid")},
        {"Bh", new ChemicalElementData("Bohrium", "Bh", 107, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1981.", "Unknown", "Solid")},
        {"Hs", new ChemicalElementData("Hassium", "Hs", 108, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1984.", "Unknown", "Solid")},
        {"Mt", new ChemicalElementData("Meitnerium", "Mt", 109, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1982.", "Unknown", "Solid")},
        {"Ds", new ChemicalElementData("Darmstadtium", "Ds", 110, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1994.", "Unknown", "Solid")},
        {"Rg", new ChemicalElementData("Roentgenium", "Rg", 111, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1994.", "Unknown", "Solid")},
        {"Cn", new ChemicalElementData("Copernicium", "Cn", 112, "Transition Metal", "Highly radioactive synthetic element.", "Discovered in 1996.", "Unknown", "Gas (predicted)")},
        {"Nh", new ChemicalElementData("Nihonium", "Nh", 113, "Post-Transition", "First element discovered in Asia.", "Discovered in 2003.", "Unknown", "Solid")},
        {"Fl", new ChemicalElementData("Flerovium", "Fl", 114, "Post-Transition", "Highly radioactive synthetic element.", "Discovered in 1998.", "Unknown", "Solid")},
        {"Mc", new ChemicalElementData("Moscovium", "Mc", 115, "Post-Transition", "Highly radioactive synthetic element.", "Discovered in 2003.", "Unknown", "Solid")},
        {"Lv", new ChemicalElementData("Livermorium", "Lv", 116, "Post-Transition", "Highly radioactive synthetic element.", "Discovered in 2000.", "Unknown", "Solid")},
        {"Ts", new ChemicalElementData("Tennessine", "Ts", 117, "Halogen", "Second heaviest known element.", "Discovered in 2010.", "Unknown", "Solid")},
        {"Og", new ChemicalElementData("Oganesson", "Og", 118, "Noble Gas", "Heaviest known element.", "Discovered in 2002.", "Unknown", "Gas/Solid (predicted)")}
    };

    public static ChemicalElementData GetElementBySymbol(string inputSymbol)
    {
        if (string.IsNullOrEmpty(inputSymbol)) return null;
        
        string formattedSymbol = inputSymbol.Substring(0, 1).ToUpper() + 
                                (inputSymbol.Length > 1 ? inputSymbol.Substring(1).ToLower() : "");

        if (Elements.ContainsKey(formattedSymbol))
        {
            return Elements[formattedSymbol];
        }
        
        return null; 
    }
}