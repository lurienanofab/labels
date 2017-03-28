using System.Collections.Generic;

namespace Labels.Models
{
    public class ChemicalItem
    {
        public int PrivateChemicalID { get; set; }
        public string ChemicalName { get; set; }
        public bool Restricted { get; set; }
        public bool Shared { get; set; }
    }

    public class ChemicalItemComparer : IEqualityComparer<ChemicalItem>
    {
        public bool Equals(ChemicalItem x, ChemicalItem y)
        {
            return x.PrivateChemicalID == y.PrivateChemicalID;
        }

        public int GetHashCode(ChemicalItem obj)
        {
            return obj.PrivateChemicalID.GetHashCode();
        }
    }
}