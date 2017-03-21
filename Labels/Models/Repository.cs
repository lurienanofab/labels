using LNF.Repository;
using LNF.Repository.Data;
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

            var query = DA.Current.Query<Client>()
                .Where(x => x.Active)
                .OrderBy(x => x.LName)
                .ThenBy(x => x.FName);

            return query.Select(x => new UserItem()
            {
                ClientID = x.ClientID,
                LName = x.LName,
                FName = x.FName,
                StartDate = GetStartDate(x.ClientID, activeLogs)
            });
        }

        public static IEnumerable<UserItem> GetManagers(int clientId)
        {
            var activeLogs = DA.Current.Query<ActiveLog>().Where(x => x.TableName == "Client").ToList();

            var query = DA.Current.Query<ClientManager>()
                .Where(x => x.Active && x.ManagerOrg.Active && x.ManagerOrg.Client.Active && x.ClientOrg.Client.ClientID == clientId)
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

        public static bool IsInternalOrg(Org org)
        {
            string internalOrgTypeName = ConfigurationManager.AppSettings["InternalOrgTypeName"];
            return org.OrgType.OrgTypeName == internalOrgTypeName;
        }

        public static DateTime GetStartDate(int clientId, IList<ActiveLog> activeLogs)
        {
            return activeLogs
                .Where(x => x.Record == clientId)
                .Min(x => x.EnableDate);
        }
    }
}