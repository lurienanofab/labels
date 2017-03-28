using System;

namespace Labels.Models
{
    public class ReportItem
    {
        public DateTime PrintDateTime { get; set; }
        public string DisplayName { get; set; }
        public string ChemicalName { get; set; }
        public bool Restricted { get; set; }
        public string LocationName { get; set; }
        public string RoomName { get; set; }
    }
}