using Labels.Models;
using System.Web.Mvc;
using System.Configuration;

namespace Labels.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("nametags")]
        public ActionResult NameTags()
        {
            return View();
        }

        [Route("exit")]
        public ActionResult ExitApplication()
        {
            string exitUrl = ConfigurationManager.AppSettings["ExitUrl"];
            return Redirect(exitUrl);
        }
    }
}