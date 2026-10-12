using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuditService _auditService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            this._auditService = _auditService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by username or email
            var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                await _auditService.LogAsync(
                    userId: null,
                    userName: model.Email,
                    action: "Failed Login",
                    entityName: "Authentication",
                    result: "Failure",
                    details: "Attempted login with non-existent account.");

                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    userName: user.Email,
                    action: "Failed Login",
                    entityName: "Authentication",
                    result: "Failure",
                    details: "Account is deactivated.");

                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            // Lockout enabled = true
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    userName: user.Email,
                    action: "Login",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Success",
                    details: "User successfully authenticated.");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    userName: user.Email,
                    action: "Failed Login",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "LockedOut",
                    details: "User account locked due to multiple consecutive failed login attempts.");

                ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again later or contact support.");
                return View(model);
            }

            await _auditService.LogAsync(
                userId: user.Id,
                userName: user.Email,
                action: "Failed Login",
                entityName: "Authentication",
                recordId: user.Id,
                result: "Failure",
                details: "Invalid password attempt.");

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role (default to SalesExecutive if not valid)
                var role = string.Equals(model.Role, ApplicationRole.Manager, StringComparison.OrdinalIgnoreCase)
                    ? ApplicationRole.Manager
                    : ApplicationRole.SalesExecutive;

                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new ApplicationRole(role));
                }

                await _userManager.AddToRoleAsync(user, role);

                await _auditService.LogAsync(
                    userId: user.Id,
                    userName: user.Email,
                    action: "Create",
                    entityName: "User",
                    recordId: user.Id,
                    result: "Success",
                    details: $"User registered with role {role}.");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(
                    userId: user.Id,
                    userName: user.Email,
                    action: "Logout",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Success",
                    details: "User logged out.");
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
