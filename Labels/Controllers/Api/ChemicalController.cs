using Labels.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;

namespace Labels.Controllers.Api
{
    public class ChemicalLabelController : ApiController
    {
        [Route("api/chemical/{room?}")]
        public IEnumerable<ChemicalItem> Get(string room = null)
        {
            return Repository.GetChemicalItems(RoomUtility.Slugs[room]);
        }

        [Route("api/chemical/{room}/{privateChemicalId}/location")]
        public IEnumerable<LocationItem> GetLocations(string room, int privateChemicalId)
        {
            return Repository.GetLocationItems(RoomUtility.Slugs[room], privateChemicalId);
        }

        [HttpPost, Route("api/chemical/log")]
        public HttpResponseMessage AddLogEntry(LogEntryModel model)
        {
            bool success = Repository.AddLogEntry(model, Request.GetOwinContext().Request.RemoteIpAddress);
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = CreateJsonContent(new { success });
            return result;
        }

        [Route("api/chemical/report")]
        public IEnumerable<ReportItem> GetReportItems(DateTime sd, DateTime ed)
        {
            return Repository.GetReportItems(sd, ed);
        }

        private ObjectContent<T> CreateJsonContent<T>(T value)
        {
            return new ObjectContent<T>(value, new JsonMediaTypeFormatter(), "application/json");
        }
    }
}
