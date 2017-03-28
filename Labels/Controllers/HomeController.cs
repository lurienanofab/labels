using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace Labels.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            Session["HideMenu"] = false;
            Session["ReturnUrl"] = VirtualPathUtility.ToAbsolute("~");
            return View();
        }

        [Route("exit")]
        public ActionResult ExitApplication()
        {
            string exitUrl = ConfigurationManager.AppSettings["ExitUrl"];
            string returnUrl = string.Empty;

            if (Session["ReturnUrl"] != null)
                returnUrl = Session["ReturnUrl"].ToString();

            return Redirect(string.Format(exitUrl, returnUrl));
        }

        [Route("app/{page}/{room?}")]
        public ActionResult Dispatch(string page, string room = null)
        {
            Session["HideMenu"] = true;

            if (page == "nametags")
            {
                Session["ReturnUrl"] = VirtualPathUtility.ToAbsolute("~/app/nametags");
                return RedirectToAction("Index", "NameTags");
            }
            else
            {
                Session["ReturnUrl"] = VirtualPathUtility.ToAbsolute(string.Format("~/app/chemicals/{0}", room));
                return RedirectToAction("Index", "Chemicals", new { room });
            }
        }
    }
}