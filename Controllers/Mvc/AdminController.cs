using System.Web.Mvc;

namespace LightSwitchApplication.Controllers.Mvc
{
	[RoutePrefix("/mvc")]
	public class AdminController : Controller
    {

		//
		// GET: /Admin
		[AcceptVerbs(HttpVerbs.Get)]
		public ActionResult Index()
		{
			return View();
		}


		//
        // GET: /Admin/users
		[AcceptVerbs(HttpVerbs.Get)]
		[Authorize(Roles = "Administrator")]
		public ActionResult Users()
        {
            return View();
        }


		//
		// GET: /Admin/roles
		[AcceptVerbs(HttpVerbs.Get)]
		[Authorize(Roles = "Administrator")]
		public ActionResult Roles()
		{
			return View();
		}


	}
}