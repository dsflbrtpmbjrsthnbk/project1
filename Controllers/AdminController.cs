using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserManagementApp.Controllers
{
    /// <summary>
    /// The admin panel is implemented as a Blazor page at /admin.
    /// This controller exists only to redirect any legacy /Admin MVC routes there.
    /// </summary>
    [Route("Admin")]
    public class AdminController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index() => Redirect("/admin");
    }
}
