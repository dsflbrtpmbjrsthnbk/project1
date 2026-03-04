using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await CheckUserAuthentication())
                return RedirectToAction("Login", "Account");

            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
                    .ThenByDescending(u => u.RegistrationTime)
                    .ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new List<User>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block([FromForm] List<Guid> selectedIds)
        {
            if (!await CheckUserAuthentication())
                return RedirectToAction("Login", "Account");

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for blocking.";
                    return RedirectToAction("Index");
                }

                var currentUserId = GetUniqIdValue();
                var usersToBlock = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in usersToBlock)
                    user.Status = "blocked";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully blocked {usersToBlock.Count} user(s).";

                if (currentUserId.HasValue && selectedIds.Contains(currentUserId.Value))
                {
                    HttpContext.Session.Clear();
                    TempData["InfoMessage"] = "You have blocked yourself and been logged out.";
                    return RedirectToAction("Login", "Account");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error blocking users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while blocking users.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock([FromForm] List<Guid> selectedIds)
        {
            if (!await CheckUserAuthentication())
                return RedirectToAction("Login", "Account");

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for unblocking.";
                    return RedirectToAction("Index");
                }

                var usersToUnblock = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id) && u.Status == "blocked")
                    .ToListAsync();

                foreach (var user in usersToUnblock)
                    user.Status = "active";

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully unblocked {usersToUnblock.Count} user(s).";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unblocking users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while unblocking users.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] List<Guid> selectedIds)
        {
            if (!await CheckUserAuthentication())
                return RedirectToAction("Login", "Account");

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for deletion.";
                    return RedirectToAction("Index");
                }

                var currentUserId = GetUniqIdValue();
                var usersToDelete = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id))
                    .ToListAsync();

                _context.Users.RemoveRange(usersToDelete);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully deleted {usersToDelete.Count} user(s).";

                if (currentUserId.HasValue && selectedIds.Contains(currentUserId.Value))
                {
                    HttpContext.Session.Clear();
                    TempData["InfoMessage"] = "You have deleted your account and been logged out.";
                    return RedirectToAction("Login", "Account");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting users.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnverified()
        {
            if (!await CheckUserAuthentication())
                return RedirectToAction("Login", "Account");

            try
            {
                var unverifiedUsers = await _context.Users
                    .Where(u => u.Status == "unverified")
                    .ToListAsync();

                if (unverifiedUsers.Count == 0)
                {
                    TempData["InfoMessage"] = "No unverified users found.";
                    return RedirectToAction("Index");
                }

                _context.Users.RemoveRange(unverifiedUsers);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully deleted {unverifiedUsers.Count} unverified user(s).";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting unverified users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting unverified users.";
                return RedirectToAction("Index");
            }
        }

        private async Task<bool> CheckUserAuthentication()
        {
            var userId = GetUniqIdValue();
            if (!userId.HasValue)
                return false;

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || user.IsBlocked())
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Your account has been blocked or deleted. Please contact administrator.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking user authentication: {ex.Message}");
                return false;
            }
        }

        private Guid? GetUniqIdValue()
        {
            var val = HttpContext.Session.GetString("UserId");
            return val != null ? Guid.Parse(val) : null;
        }
    }
}
