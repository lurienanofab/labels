using System;
using System.Collections.Generic;
using LNF.Repository.Inventory;

namespace Labels.Models
{
    public class ChemicalLabel
    {
        public ChemicalLocation Location { get; set; }
        public PrivateChemical Chemical { get; set; }

        public ChemicalItem CreateChemicalItem()
        {
            return new ChemicalItem()
            {
                PrivateChemicalID = Chemical.PrivateChemicalID,
                ChemicalName = Chemical.ChemicalName.Trim(),
                Restricted = Chemical.Restricted,
                Shared = Chemical.Shared
            };
        }

        public LocationItem CreateLocationItem()
        {
            return new LocationItem()
            {
                LabelLocationID = Location.LabelLocation.LabelLocationID,
                LocationName = Location.LabelLocation.LocationName,
                RoomName = RoomUtility.Rooms[Location.LabelLocation.LabelRoom.Slug]
            };
        }
    }
}