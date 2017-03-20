using Labels.Models;
using System.Collections.Generic;
using System.Web.Http;

namespace Labels.Controllers
{
    public class UserController : ApiController
    {
        [Route("api/user")]
        public IEnumerable<UserItem> Get()
        {
            return Repository.GetActiveUsers();
        }

        [Route("api/user/{clientId}/manager")]
        public IEnumerable<UserItem> GetManagers(int clientId)
        {
            return Repository.GetManagers(clientId);
        }

        [Route("api/user/{clientId}/manager/{managerId}/org")]
        public IEnumerable<OrgItem> GetManagerOrgs(int clientId, int managerId)
        {
            return Repository.GetManagerOrgs(clientId, managerId);
        }
    }
}
