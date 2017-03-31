using LNF;
using LNF.Cache;
using LNF.Email;
using LNF.Repository;
using LNF.Repository.Data;
using LNF.Repository.Inventory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Labels.Models
{
    public static class Repository
    {
        public static IEnumerable<UserItem> GetActiveUsers()
        {
            var activeLogs = DA.Current.Query<ActiveLog>().Where(x => x.TableName == "Client").ToList();

            var query = DA.Current.Query<ClientInfo>()
                .Where(x => x.ClientActive)
                .OrderBy(x => x.LName)
                .ThenBy(x => x.FName);

            return query.Select(x => new UserItem()
            {
                ClientID = x.ClientID,
                LName = x.LName,
                FName = x.FName,
                Phone = x.Phone,
                Email = x.Email,
                StartDate = GetStartDate(x.ClientID, activeLogs)
            });
        }

        public static bool AddLogEntry(LogEntryModel model, string ip)
        {
            var cloc = DA.Current.Query<ChemicalLocation>().FirstOrDefault(x => x.PrivateChemical.PrivateChemicalID == model.PrivateChemicalID && x.LabelLocation.LabelLocationID == model.LabelLocationID);

            if (cloc == null)
                throw new InvalidOperationException(string.Format("Cannot find a chemical location with PrivateChemicalID = {0} and LabelLocationID = {1}", model.PrivateChemicalID, model.LabelLocationID));

            var log = new ChemicalLabelPrintLog()
            {
                ChemicalLocation = cloc,
                Client = DA.Current.Single<Client>(model.LabelClientID),
                PrintDateTime = DateTime.Now,
                IPAddress = ip
            };

            DA.Current.Insert(log);

            if (cloc.PrivateChemical.Restricted)
            {
                var printedBy = model.GetPrintedBy();

                var sendResult = Providers.Email.SendMessage(new SendMessageArgs()
                {
                    Caller = "Labels.Models.Repository.AddLogEntry",
                    ClientID = printedBy.ClientID,
                    DisplayName = printedBy.DisplayName,
                    From = printedBy.PrimaryEmail(),
                    To = new[] { ConfigurationManager.AppSettings["AdminEmail"] },
                    Subject = "Label for restricted chemical printed by user",
                    Body = "<h2>A label for a restricted chemical was printed.</h2>"
                        + "<ul>"
                        + string.Format("<li><b>DateTime:</b> {0}</li>", log.PrintDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                        + string.Format("<li><b>User:</b> {0}</li>", log.Client.DisplayName)
                        + string.Format("<li><b>Printed By:</b> {0}</li>", printedBy.DisplayName)
                        + string.Format("<li><b>Phone:</b> {0}</li>", model.ContactPhone)
                        + string.Format("<li><b>Email:</b> {0}</li>", model.ContactEmail)
                        + string.Format("<li><b>Chemical:</b> {0}</li>", cloc.PrivateChemical.ChemicalName)
                        + string.Format("<li><b>Location:</b> {0} [{1}]</li>", cloc.LabelLocation.LocationName, RoomUtility.Rooms[cloc.LabelLocation.LabelRoom.Slug])
                        + "</ul>",
                    IsHtml = true
                });

                return sendResult.Success;
            }

            return true;
        }

        public static IEnumerable<UserItem> GetManagers(int clientId)
        {
            var activeLogs = DA.Current.Query<ActiveLog>().Where(x => x.TableName == "Client").ToList();

            var query = DA.Current.Query<ClientManager>()
                .Where(x => x.Active && x.ManagerOrg.Active && x.ManagerOrg.Client.Active && x.ManagerOrg.IsManager && x.ClientOrg.Client.ClientID == clientId)
                .OrderBy(x => x.ManagerOrg.Client.LName).ThenBy(x => x.ManagerOrg.Client.FName);

            return query.Select(x => new UserItem()
            {
                ClientID = x.ManagerOrg.Client.ClientID,
                LName = x.ManagerOrg.Client.LName,
                FName = x.ManagerOrg.Client.FName,
                StartDate = GetStartDate(x.ManagerOrg.Client.ClientID, activeLogs)
            });
        }

        public static IEnumerable<OrgItem> GetClientOrgs(int clientId)
        {
            var query = DA.Current.Query<ClientOrg>().Where(x => x.Client.ClientID == clientId && x.Active);

            return query.Select(x => new OrgItem()
            {
                OrgID = x.Org.OrgID,
                OrgName = x.Org.OrgName,
                Internal = IsInternalOrg(x.Org)
            });
        }

        public static string DeletePrivateChemical(int privateChemicalId)
        {
            var pc = DA.Current.Single<PrivateChemical>(privateChemicalId);

            if (pc == null)
                throw new ArgumentException(string.Format("Cannot find a PrivateChemical with PrivateChemicalID = {0}", privateChemicalId));

            pc.Deleted = true;

            if (pc.ApprovedByClientID == -1)
            {
                // reject
                Providers.Email.SendMessage(new SendMessageArgs()
                {
                    Caller = "Labels.Models.Repository.DeletePrivateChemical",
                    ClientID = CacheManager.Current.ClientID,
                    From = ConfigurationManager.AppSettings["AdminEmail"],
                    To = new[] { pc.RequestedByClient.PrimaryEmail() },
                    Subject = "Private chemical request rejected",
                    Body = string.Format("Your request to add a new private chemical has been rejected{0}{0}Chemical name: {1}{0}{0}If you have any questions please reply to this email.", "<br>", pc.ChemicalName),
                    IsHtml = true
                });
            }

            return pc.ChemicalName;
        }

        public static IEnumerable<OrgItem> GetManagerOrgs(int clientId, int managerId)
        {
            var managers = DA.Current.Query<ClientManager>()
                .Where(x => x.Active && x.ManagerOrg.Active && x.ManagerOrg.Client.Active && x.ClientOrg.Client.ClientID == clientId)
                .OrderBy(x => x.ManagerOrg.Client.LName).ThenBy(x => x.ManagerOrg.Client.FName)
                .ToList();

            return managers
                .Where(x => x.ManagerOrg.Client.ClientID == managerId)
                .Select(x => new OrgItem()
                {
                    OrgID = x.ManagerOrg.Org.OrgID,
                    OrgName = x.ManagerOrg.Org.OrgName,
                    Internal = IsInternalOrg(x.ManagerOrg.Org)
                });
        }

        private static bool IsInternalOrg(Org org)
        {
            string internalOrgTypeName = ConfigurationManager.AppSettings["InternalOrgTypeName"];
            return org.OrgType.OrgTypeName == internalOrgTypeName;
        }

        private static DateTime? GetStartDate(int clientId, IList<ActiveLog> activeLogs)
        {
            return activeLogs.Where(x => x.Record == clientId).Min(x => (DateTime?)x.EnableDate);
        }

        public static IEnumerable<ChemicalItem> GetChemicalItems(string slug)
        {
            var join = SelectChemicalLabels(slug);

            return join.ToList().Select(x => x.CreateChemicalItem()).Distinct(new ChemicalItemComparer()).OrderBy(x => x.ChemicalName);
        }

        public static IEnumerable<LocationItem> GetLocationItems(string slug, int privateChemicalId)
        {
            var result = SelectChemicalLabels(slug)
                .Where(x => x.Chemical.PrivateChemicalID == privateChemicalId)
                .Select(x => x.CreateLocationItem())
                .OrderBy(x => x.LocationName);

            return result;
        }

        private static IEnumerable<ChemicalLabel> SelectChemicalLabels(string slug)
        {
            var chems = DA.Current.Query<PrivateChemical>();
            var locs = DA.Current.Query<ChemicalLocation>();

            var result = locs.Join(
                inner: chems,
                outerKeySelector: o => o.PrivateChemical.PrivateChemicalID,
                innerKeySelector: i => i.PrivateChemicalID,
                resultSelector: (o, i) => new ChemicalLabel { Location = o, Chemical = i }
            ).Where(x => !x.Chemical.Deleted);

            if (!string.IsNullOrEmpty(slug))
                result = result.Where(x => x.Location.LabelLocation.LabelRoom.Slug == slug);

            return result;
        }

        public static IList<PrivateChemical> GetPrivateChemicals()
        {
            return DA.Current.Query<PrivateChemical>().Where(x => !x.Deleted).ToList();
        }

        public static void AddPrivateChemical(ManageEditModel model, int clientId)
        {
            PrivateChemical pc = new PrivateChemical()
            {
                ApprovedByClientID = clientId,
                ApprovedDate = DateTime.Now,
                ChemicalName = model.ChemicalName,
                Deleted = false,
                MsdsUrl = model.MsdsUrl,
                Notes = model.Notes,
                RequestedByClient = DA.Current.Single<Client>(model.RequestedByClientID),
                Restricted = model.Restricted,
                Shared = model.Shared
            };

            DA.Current.Insert(pc);

            SaveLocations(pc, model.GetSelectedLocations());
        }

        public static void UpdatePrivateChemical(ManageEditModel model, int clientId)
        {
            var pc = DA.Current.Single<PrivateChemical>(model.PrivateChemicalID);

            if (pc != null)
            {
                var requestor = model.GetRequestedBy();

                if (pc.ApprovedByClientID == -1)
                {
                    pc.ApprovedByClientID = clientId;
                    pc.ApprovedDate = DateTime.Now;

                    Providers.Email.SendMessage(new SendMessageArgs()
                    {
                        Caller = "Labels.Models.Repository.UpdatePrivateChemical",
                        ClientID = CacheManager.Current.ClientID,
                        From = ConfigurationManager.AppSettings["AdminEmail"],
                        To = new[] { requestor.PrimaryEmail() },
                        Subject = "Private chemical request approved",
                        Body = string.Format("Your request to add a new private chemical has been approved{0}{0}Chemical name: {1}{0}{0}If you have any questions please reply to this email.", "<br>", model.ChemicalName),
                        IsHtml = true
                    });
                }

                pc.ChemicalName = model.ChemicalName;
                pc.Deleted = false;
                pc.MsdsUrl = model.MsdsUrl;
                pc.Notes = model.Notes;
                pc.RequestedByClient = DA.Current.Single<Client>(model.RequestedByClientID);
                pc.Restricted = model.Restricted;
                pc.Shared = model.Shared;

                SaveLocations(pc, model.GetSelectedLocations());
            }
        }

        public static void RequestPrivateChemical(ManageEditModel model)
        {
            var requestor = model.GetRequestedBy();

            PrivateChemical pc = new PrivateChemical()
            {
                ApprovedByClientID = -1,
                ApprovedDate = null,
                ChemicalName = model.ChemicalName,
                Deleted = false,
                MsdsUrl = model.MsdsUrl,
                Notes = model.Notes,
                RequestedByClient = requestor,
                Restricted = model.Restricted,
                Shared = model.Shared
            };

            DA.Current.Insert(pc);

            SaveLocations(pc, model.GetSelectedLocations());

            var requestUrl = Providers.Context.Current.GetRequestUrl();
            var host = requestUrl.GetLeftPart(UriPartial.Authority);

            string approveUrl = string.Format("{0}/labels/chemicals/manage/edit/{1}", host, pc.PrivateChemicalID);
            string rejectUrl = string.Format("{0}/labels/chemicals/manage/delete/{1}", host, pc.PrivateChemicalID);

            Providers.Email.SendMessage(new SendMessageArgs()
            {
                Caller = "Labels.Models.Repository.RequestPrivateChemical",
                ClientID = CacheManager.Current.ClientID,
                From = requestor.PrimaryEmail(),
                DisplayName = string.Format("{0} {1}", requestor.FName, requestor.LName),
                To = new[] { ConfigurationManager.AppSettings["AdminEmail"] },
                Subject = "New private chemical request",
                Body = string.Format("A new private chemical has been requested by {1}{0}{0}Chemical name: {2}{0}{0}<a href=\"{3}\">Click here to approve</a>{0}<a href=\"{4}\">Click here to reject</a>", "<br>", requestor.DisplayName, model.ChemicalName, approveUrl, rejectUrl),
                IsHtml = true
            });
        }

        public static void SaveLocations(PrivateChemical pc, int[] locations)
        {
            // delete all previous locations for this PrivateChemical
            var existing = DA.Current.Query<ChemicalLocation>().Where(x => x.PrivateChemical == pc);
            DA.Current.Delete(existing);

            // select the locations
            var labelLocs = DA.Current.Query<LabelLocation>().Where(x => locations.Contains(x.LabelLocationID)).ToList();

            // add a ChemicalLocation for each
            foreach (int id in locations)
            {
                var ll = labelLocs.FirstOrDefault(x => x.LabelLocationID == id);
                if (ll != null)
                {
                    DA.Current.Insert(new ChemicalLocation()
                    {
                        PrivateChemical = pc,
                        LabelLocation = ll
                    });
                }
            }
        }

        public static string GetApprovedBy(PrivateChemical pc)
        {
            if (pc == null)
                return string.Empty;

            var client = DA.Current.Single<Client>(pc.ApprovedByClientID);

            if (client == null)
                return string.Empty;

            return client.DisplayName;
        }

        public static string GetApprovedDate(PrivateChemical pc)
        {
            if (pc == null)
                return string.Empty;

            if (!pc.ApprovedDate.HasValue)
                return string.Empty;

            return pc.ApprovedDate.Value.ToString("MM/dd/yyyy");
        }

        public static IEnumerable<ReportItem> GetReportItems(DateTime sd, DateTime ed)
        {
            var query = DA.Current.Query<ChemicalLabelPrintLog>()
                .Where(x => x.PrintDateTime >= sd && x.PrintDateTime < ed)
                .OrderBy(x => x.PrintDateTime);

            return query.ToList().Select(x => new ReportItem()
            {
                PrintDateTime = x.PrintDateTime,
                DisplayName = x.Client.DisplayName,
                ChemicalName = x.ChemicalLocation.PrivateChemical.ChemicalName,
                Restricted = x.ChemicalLocation.PrivateChemical.Restricted,
                LocationName = x.ChemicalLocation.LabelLocation.LocationName,
                RoomName = RoomUtility.Rooms[x.ChemicalLocation.LabelLocation.LabelRoom.Slug]
            });
        }
    }
}