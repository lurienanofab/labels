using LNF.Cache;
using LNF.Repository;
using LNF.Repository.Data;
using LNF.Repository.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Labels.Models
{
    public class ManageEditModel
    {
        public int PrivateChemicalID { get; set; }
        public string ChemicalName { get; set; }
        public string SelectedLocations { get; set; }
        public int RequestedByClientID { get; set; }
        public string MsdsUrl { get; set; }
        public string Notes { get; set; }
        public bool Restricted { get; set; }
        public bool Shared { get; set; }
        public PrivateChemical PrivateChemical { get; set; }
        public IList<ChemicalLocation> Locations { get; set; }
        public IEnumerable<UserItem> Users { get; set; }

        public IEnumerable<LocationItem> GetLocations(string room)
        {
            var slug = RoomUtility.Slugs[room];

            var result = DA.Current
                .Query<LabelLocation>().Where(x => x.LabelRoom.Slug == slug)
                .ToList()
                .Select(x => new LocationItem()
                {
                    LabelLocationID = x.LabelLocationID,
                    LocationName = x.LocationName,
                    RoomName = RoomUtility.Rooms[x.LabelRoom.Slug]
                });

            return result;
        }

        public void Init()
        {
            PrivateChemical = DA.Current.Single<PrivateChemical>(PrivateChemicalID);

            if (PrivateChemical != null)
            {
                ChemicalName = PrivateChemical.ChemicalName;
                Locations = DA.Current.Query<ChemicalLocation>().Where(x => x.PrivateChemical.PrivateChemicalID == PrivateChemicalID).ToList();
                SelectedLocations = string.Join(",", Locations.Select(x => x.LabelLocation.LabelLocationID));
                RequestedByClientID = PrivateChemical.RequestedByClient.ClientID;
                MsdsUrl = PrivateChemical.MsdsUrl;
                Notes = PrivateChemical.Notes;
                Restricted = PrivateChemical.Restricted;
                Shared = PrivateChemical.Shared;
            }
            else
            {
                ChemicalName = string.Empty;
                Locations = null;
                SelectedLocations = string.Empty;
                RequestedByClientID = CacheManager.Current.ClientID;
                MsdsUrl = string.Empty;
                Notes = string.Empty;
                Restricted = false;
                Shared = false;
            }

            Users = Repository.GetActiveUsers();
        }

        public int[] GetSelectedLocations()
        {
            if (string.IsNullOrEmpty(SelectedLocations))
                return new int[] { };
            else
                return SelectedLocations.Split(',').Select(x => int.Parse(x)).ToArray();
        }

        public Client GetRequestedBy()
        {
            var result = DA.Current.Single<Client>(RequestedByClientID);

            if (result == null)
                throw new InvalidOperationException(string.Format("Cannot find a client with RequestedByClientID = {0}", RequestedByClientID));

            return result;
        }
    }
}