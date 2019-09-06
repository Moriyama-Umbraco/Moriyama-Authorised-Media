using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Moriyama.AuthorisedMedia.Web.Controllers
{
    class TestSurfaceController : SurfaceController
    {
        public ActionResult Login()
        {
            Members.Login("darren@moriyama.co.uk", "abc123abc123");
            return Redirect("/");
        }
    }
}
