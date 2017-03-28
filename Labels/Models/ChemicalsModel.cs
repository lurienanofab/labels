using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Labels.Models
{
    public class ChemicalsModel
    {
        public int ClientID { get; set; }
        public bool IsStaff { get; set; }
        public string Room { get; set; }
        public DateTime DisposeDate { get; set; }
        public DateTime StartDate { get; set; }
        public string ErrorMessage { get; set; }
        public string ApiUrl { get; set; }
        public string LabelUrl { get; set; }
        public string DisplayName { get; set; }

        public string GetRoomName()
        {
            return RoomUtility.Rooms[Room];
        }
    }
}