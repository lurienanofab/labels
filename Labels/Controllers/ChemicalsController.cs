using Labels.Models;
using LNF.Cache;
using LNF.Models.Data;
using LNF.Repository;
using LNF.Repository.Inventory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Labels.Controllers
{
    public class ChemicalsController : Controller
    {
        [Route("chemicals/{Room?}")]
        public ActionResult Index(ChemicalsModel model)
        {
            CacheManager.Current.CheckSession();

            model.ErrorMessage = string.Empty;
            if (!string.IsNullOrEmpty(model.Room) && !Models.RoomUtility.Rooms.ContainsKey(model.Room))
                model.ErrorMessage = "Invalid room.";
            else
                Session["room"] = model.Room;

            model.ApiUrl = Url.Content("~/api/");
            model.LabelUrl = Url.Content("~/Content/labels/");

            model.ClientID = CacheManager.Current.ClientID;
            model.IsStaff = CacheManager.Current.CurrentUser.HasPriv(ClientPrivilege.Staff);

            model.StartDate = DateTime.Now.Date;

            DateTime dd = DateTime.Now.Date.AddMonths(1);
            DateTime fom = new DateTime(dd.Year, dd.Month, 1);
            model.DisposeDate = fom.AddYears(1);

            model.DisplayName = CacheManager.Current.CurrentUser.DisplayName;

            ViewBag.Room = GetRoomFromSession();

            return View(model);
        }

        [Route("chemicals/manage")]
        public ActionResult Manage(ManageModel model)
        {
            CacheManager.Current.CheckSession();
            bool isStaff = CacheManager.Current.CurrentUser.IsStaff();
            model.Chemicals = Repository.GetPrivateChemicals();
            ViewBag.Room = GetRoomFromSession();
            ViewBag.IsStaff = isStaff;
            return View(model);
        }

        [Route("chemicals/manage/edit/{PrivateChemicalID?}")]
        public ActionResult ManageEdit(ManageEditModel model)
        {
            CacheManager.Current.CheckSession();
            ViewBag.Room = GetRoomFromSession();
            model.Init();
            return View(model);
        }

        [Route("chemicals/manage/edit/{PrivateChemicalID?}/update")]
        public ActionResult ManageEditUpdate(ManageEditModel model)
        {
            string errmsg = string.Empty;
            int errors = 0;

            if (string.IsNullOrEmpty(model.ChemicalName))
            {
                errmsg += "<li>Missing chemical name.</li>";
                errors++;
            }

            if (model.GetSelectedLocations().Length == 0)
            {
                errmsg += "<li>At least one location must be selected.</li>";
                errors++;
            }

            if (errors > 0)
            {
                ViewBag.ErrorMessage = string.Format("<ul>{0}</ul>", errmsg);
                ViewBag.Room = GetRoomFromSession();
                model.Users = Repository.GetActiveUsers();
                return View("ManageEdit", model);
            }

            CacheManager.Current.CheckSession();

            bool isStaff = CacheManager.Current.CurrentUser.IsStaff();

            if (isStaff)
            {
                // adding or updating
                if (model.PrivateChemicalID == 0)
                {
                    Repository.AddPrivateChemical(model, CacheManager.Current.ClientID);
                }
                else
                {
                    Repository.UpdatePrivateChemical(model, CacheManager.Current.ClientID);
                }
            }
            else
            {
                // requesting
                if (model.PrivateChemicalID == 0)
                {
                    model.RequestedByClientID = CacheManager.Current.ClientID;
                    Repository.RequestPrivateChemical(model);
                }
            }

            return RedirectToAction("Manage", "Chemicals");
        }

        [Route("chemicals/manage/delete/{privateChemicalId}")]
        public ActionResult ManageDelete(int privateChemicalId)
        {
            CacheManager.Current.CheckSession();

            bool isStaff = CacheManager.Current.CurrentUser.IsStaff();

            ViewBag.ErrorMessage = string.Empty;
            ViewBag.DeletedChemicalName = string.Empty;
            ViewBag.Room = GetRoomFromSession();
            ViewBag.IsStaff = isStaff;

            if (isStaff)
            {
                string chemicalName = Repository.DeletePrivateChemical(privateChemicalId);
                ViewBag.DeletedChemicalName = chemicalName;
            }
            else
            {
                ViewBag.ErrorMessage = "You do not have permission to delete a private chemical.";
            }

            return View();
        }

        [Route("chemicals/report")]
        public ActionResult Report(DateTime? sd = null, DateTime? ed = null)
        {
            if (!sd.HasValue || !ed.HasValue)
            {
                ed = DateTime.Now.Date.AddDays(1);
                sd = ed.Value.AddDays(-7);
            }

            var model = new ReportModel();
            model.StartDate = sd.Value;
            model.EndDate = ed.Value;

            ViewBag.Room = GetRoomFromSession();

            return View(model);
        }

        [Route("chemicals/report/export/{format}")]
        public ActionResult ReportExport(string format, DateTime sd, DateTime ed)
        {
            var items = Repository.GetReportItems(sd, ed);
            switch (format)
            {
                case "csv":
                    return File(Encoding.UTF8.GetBytes(GetCsvContent(items)), "text/csv", string.Format("LabelPrintLog-{0:yyyyMMdd}-{1:yyyyMMdd}.csv", sd, ed));
                case "xml":
                    return File(Encoding.UTF8.GetBytes(GetXmlContent(items)), "text/xml", string.Format("LabelPrintLog-{0:yyyyMMdd}-{1:yyyyMMdd}.xml", sd, ed));
                case "json":
                    return File(Encoding.UTF8.GetBytes(GetJsonContent(items)), "application/json", string.Format("LabelPrintLog-{0:yyyyMMdd}-{1:yyyyMMdd}.json", sd, ed));
                default:
                    throw new NotImplementedException(string.Format("Not implemented: {0}", format));
            }
        }

        private string GetRoomFromSession()
        {
            string result = null;

            if (Session["room"] != null)
                result = Session["room"].ToString();

            return result;
        }

        private string GetCsvContent(IEnumerable<ReportItem> items)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("\"PrintDateTime\",\"DisplayName\",\"ChemicalName\",\"Restricted\",\"LocationName\",\"RoomName\"");
            foreach (var item in items)
            {
                string line = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",
                    item.PrintDateTime.ToString("M/d/yyyy h:mm:ss tt"),
                    item.DisplayName.Replace("\"", "\"\""),
                    item.ChemicalName.Replace("\"", "\"\""),
                    item.Restricted ? "TRUE" : "FALSE",
                    item.LocationName.Replace("\"", "\"\""),
                    item.RoomName.Replace("\"", "\"\""));
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private string GetXmlContent(IEnumerable<ReportItem> items)
        {
            var xdoc = new XDocument(new XElement("data", new XAttribute("name", "LabelPrintLog")));
            var table = new XElement("table", new XAttribute("name", "ReportItems"));

            foreach (var item in items)
            {
                var row = new XElement("row",
                    new XElement("PrintDateTime", item.PrintDateTime.ToString("M/d/yyyy h:mm:ss tt")),
                    new XElement("DisplayName", item.DisplayName),
                    new XElement("ChemicalName", item.ChemicalName),
                    new XElement("Restricted", item.Restricted ? "True" : "False"),
                    new XElement("LocationName", item.LocationName),
                    new XElement("RoomName", item.RoomName)
                );

                table.Add(row);
            }

            xdoc.Root.Add(table);

            return xdoc.ToString();
        }

        private string GetJsonContent(IEnumerable<ReportItem> items)
        {
            return JsonConvert.SerializeObject(items, Formatting.Indented);
        }
    }
}