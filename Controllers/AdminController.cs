using Microsoft.AspNetCore.Mvc;

namespace UserManagementApp.Controllers
{
    // Intentionally minimal — the Admin Panel lives at /admin (Blazor page: Pages/Admin.razor)
    // No redirect needed; this file is kept only to avoid missing-route 404s for any stale links.
    [Route("Admin")]
    public class AdminController : Controller
    {
        // No action — Blazor MapFallbackToPage handles /admin directly.
    }
}
