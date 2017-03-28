using System;
using LNF.Repository;
using LNF.Repository.Data;

namespace Labels.Models
{
    public class LogEntryModel
    {
        public int PrintedByClientID { get; set; }
        public int LabelClientID { get; set; }
        public int PrivateChemicalID { get; set; }
        public int LabelLocationID { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DisposeDate { get; set; }
        public bool StoreStock { get; set; }

        public Client GetPrintedBy()
        {
            var result = DA.Current.Single<Client>(PrintedByClientID);

            if (result == null)
                throw new InvalidOperationException(string.Format("Cannot find a client with PrintedByClientID = {0}", PrintedByClientID));

            return result;
        }
    }
}