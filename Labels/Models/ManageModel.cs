using LNF.Repository.Inventory;
using System.Collections.Generic;
using LNF.Repository;
using LNF.Repository.Data;

namespace Labels.Models
{
    public class ManageModel
    {
        public IList<PrivateChemical> Chemicals { get; set; }
    }
}