using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = ApplicationRole.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditService _auditService;
        private readonly IUserScopeService _userScopeService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IAuditService auditService,
            IUserScopeService userScopeService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _userScopeService = userScopeService;
        }

        // GET: Users
        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter)
        {
            var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
            var modelList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                string primaryRole = roles.FirstOrDefault() ?? "No Role";

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim().ToLower();
                    if (!user.FullName.ToLower().Contains(term) && !user.Email!.ToLower().Contains(term))
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(roleFilter) && primaryRole != roleFilter)
                {
                    continue;
                }

                bool isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                modelList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = primaryRole,
                    IsActive = user.IsActive,
                    IsLockedOut = isLocked,
                    LockoutEnd = user.LockoutEnd,
                    CreatedDate = user.CreatedDate
                });
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.RoleFilter = roleFilter;
            return View(modelList);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "A user with this email address already exists.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim().ToLower(),
                    Email = model.Email.Trim().ToLower(),
                    FullName = model.FullName.Trim(),
                    EmailConfirmed = true,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    await _auditService.LogAsync(
                        userId: _userScopeService.GetCurrentUserId(),
                        userName: _userScopeService.GetCurrentUserName(),
                        action: "Create",
                        entityName: "User",
                        recordId: user.Id,
                        newValue: $"Name: {user.FullName}, Email: {user.Email}, Role: {model.Role}",
                        result: "Success",
                        details: $"User '{user.FullName}' ({model.Role}) created by Administrator.");

                    TempData["Success"] = $"User '{user.FullName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                string oldInfo = $"Name: {user.FullName}, Email: {user.Email}, Active: {user.IsActive}";

                user.FullName = model.FullName.Trim();
                user.IsActive = model.IsActive;

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    // Update Role
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(model.Role))
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, model.Role);

                        await _auditService.LogAsync(
                            userId: _userScopeService.GetCurrentUserId(),
                            userName: _userScopeService.GetCurrentUserName(),
                            action: "Role Change",
                            entityName: "User",
                            recordId: user.Id,
                            oldValue: string.Join(",", currentRoles),
                            newValue: model.Role,
                            result: "Success",
                            details: $"User role updated to '{model.Role}'.");
                    }

                    string newInfo = $"Name: {user.FullName}, Email: {user.Email}, Active: {user.IsActive}";

                    await _auditService.LogAsync(
                        userId: _userScopeService.GetCurrentUserId(),
                        userName: _userScopeService.GetCurrentUserName(),
                        action: "Update",
                        entityName: "User",
                        recordId: user.Id,
                        oldValue: oldInfo,
                        newValue: newInfo,
                        result: "Success",
                        details: $"User '{user.FullName}' updated.");

                    TempData["Success"] = $"User '{user.FullName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            return View(model);
        }

        // POST: Users/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent self-deactivation
            if (user.Id == _userScopeService.GetCurrentUserId())
            {
                TempData["Error"] = "You cannot deactivate your own administrative account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Security",
                entityName: "User",
                recordId: user.Id,
                newValue: $"IsActive: {user.IsActive}",
                result: "Success",
                details: $"User '{user.FullName}' was {(user.IsActive ? "activated" : "deactivated")}.");

            TempData["Success"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Unlock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Security",
                entityName: "User",
                recordId: user.Id,
                result: "Success",
                details: $"Account for '{user.FullName}' was manually unlocked by administrator.");

            TempData["Success"] = $"Account for '{user.FullName}' has been unlocked.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/ResetPassword/5
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(new UserResetPasswordAdminViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            });
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordAdminViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    await _auditService.LogAsync(
                        userId: _userScopeService.GetCurrentUserId(),
                        userName: _userScopeService.GetCurrentUserName(),
                        action: "Security",
                        entityName: "User",
                        recordId: user.Id,
                        result: "Success",
                        details: $"Administrative password reset completed for user '{user.FullName}'.");

                    TempData["Success"] = $"Password for '{user.FullName}' was reset successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            return View(model);
        }
    }
}
