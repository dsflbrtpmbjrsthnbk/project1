using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // ── Email/Password Login & Register ───────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect(returnUrl ?? "/");
                
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Email and password are required.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid email or password.";
                return View();
            }

            if (user.IsBlocked())
            {
                TempData["ErrorMessage"] = "Your account has been blocked. Please contact an administrator.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                user.LastLoginTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("User {Email} logged in", email);
                return LocalRedirect(returnUrl ?? "/");
            }
            
            TempData["ErrorMessage"] = "Invalid email or password.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect("/");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return View();
            }

            var isFirstUser = !_userManager.Users.Any();
            
            var user = new User
            {
                Name = name.Trim(),
                UserName = email.ToLower().Trim(), // Required by Identity
                Email = email.ToLower().Trim(),
                Status = "active",
                RegistrationTime = DateTime.UtcNow,
                IsAdmin = isFirstUser
            };

            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User registered: {Email}", user.Email);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect("/");
            }

            foreach (var error in result.Errors)
            {
                if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                {
                    TempData["ErrorMessage"] = "This email is already registered.";
                    return View();
                }
            }
            
            TempData["ErrorMessage"] = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        // ── OAuth (Google / Facebook) ──────────────────────────────────────────

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                _logger.LogError("Error from external provider: {RemoteError}", remoteError);
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return RedirectToAction("Login", new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Error loading external login information.");
                TempData["ErrorMessage"] = "Error loading external login information.";
                return RedirectToAction("Login", new { returnUrl });
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "User";
            var loginProvider = info.LoginProvider;
            var providerKey = info.ProviderKey;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Could not retrieve email from external provider {LoginProvider}.", loginProvider);
                TempData["ErrorMessage"] = "Could not retrieve your email from the external provider.";
                return RedirectToAction("Login", new { returnUrl });
            }

            // 1. Try to sign in the user if the external login already exists
            var signInResult = await _signInManager.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent: true, bypassTwoFactor: true);
            
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation("User {Email} logged in with external provider {LoginProvider}.", user.Email, loginProvider);
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    _logger.LogError("External login succeeded but user not found for {LoginProvider} with key {ProviderKey}.", loginProvider, providerKey);
                    TempData["ErrorMessage"] = "Error retrieving user after successful external login.";
                    return RedirectToAction("Login", new { returnUrl });
                }
            }

            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("User account locked out for external login {LoginProvider}.", loginProvider);
                TempData["ErrorMessage"] = "Your account has been locked out.";
                return RedirectToAction("Login", new { returnUrl });
            }

            // 2. If external login sign-in failed, check if a user with this email already exists
            _logger.LogInformation("External login {LoginProvider} sign-in failed for email {Email}. Attempting to link or create user.", loginProvider, email);
            var userByEmail = await _userManager.FindByEmailAsync(email);

            if (userByEmail == null)
            {
                // 3. No existing user with this email, create a new user
                _logger.LogInformation("No existing user found for email {Email}. Creating new user.", email);
                var isFirstUser = !_userManager.Users.Any();
                var newUser = new User
                {
                    Name = name,
                    UserName = email.ToLower(),
                    Email = email.ToLower(),
                    Status = "active",
                    RegistrationTime = DateTime.UtcNow,
                    IsAdmin = isFirstUser,
                    EmailConfirmed = true, // External providers usually confirm email
                    LastLoginTime = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Error creating user account for {Email}: {Errors}", email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Error creating user account.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                // Link the external login to the newly created user
                var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogError("Error adding external login {LoginProvider} for new user {Email}: {Errors}", loginProvider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Error associating external login with new account.";
                    return RedirectToAction("Login", new { returnUrl });
                }

                await _signInManager.SignInAsync(newUser, isPersistent: true);
                _logger.LogInformation("New user {Email} created and logged in with external provider {LoginProvider}.", email, loginProvider);
                return LocalRedirect(returnUrl);
            }
            else
            {
                // 4. User with this email already exists, link the external login to this user
                _logger.LogInformation("Existing user found for email {Email}. Attempting to link external login {LoginProvider}.", email, loginProvider);

                if (userByEmail.IsBlocked())
                {
                    _logger.LogWarning("Attempted external login for blocked user {Email}.", email);
                    TempData["ErrorMessage"] = "Your account has been blocked.";
                    return RedirectToAction("Login");
                }

                var addLoginResult = await _userManager.AddLoginAsync(userByEmail, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(userByEmail, isPersistent: true);
                    userByEmail.LastLoginTime = DateTime.UtcNow;
                    await _userManager.UpdateAsync(userByEmail);
                    _logger.LogInformation("External login {LoginProvider} linked to existing user {Email} and logged in.", loginProvider, email);
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    _logger.LogError("Error adding external login {LoginProvider} to existing user {Email}: {Errors}", loginProvider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Error associating external login with your existing account.";
                    return RedirectToAction("Login", new { returnUrl });
                }
            }
        }

        // ── Logout ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login");
        }
        
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
