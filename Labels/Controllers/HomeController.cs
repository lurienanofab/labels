using Labels.Models;
using System.Web.Mvc;

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
            return Redirect("/sselonline");
        }
    }
}