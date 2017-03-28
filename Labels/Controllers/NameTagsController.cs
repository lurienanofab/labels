using System.Web.Mvc;

namespace Labels.Controllers
{
    public class NameTagsController : Controller
    {
        [Route("nametags")]
        public ActionResult Index()
        {
            return View();
        }
    }
}