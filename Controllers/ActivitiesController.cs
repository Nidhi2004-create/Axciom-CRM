using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
            _userManager = userManager;
        }

        // GET: Activities
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? typeFilter,
            string? statusFilter,
            string? assignedUserFilter,
            DateTime? dateFilter,
            int page = 1)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .AsQueryable();

            query = _userScopeService.ApplyActivityScope(query);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                         (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(a => a.ActivityType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(a => a.AssignedTo == assignedUserFilter);
            }

            if (dateFilter.HasValue)
            {
                var d = dateFilter.Value.Date;
                query = query.Where(a => a.ActivityDate.Date == d);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserFilter);

            var viewModel = new ActivityIndexViewModel
            {
                Activities = activities,
                SearchTerm = searchTerm,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                AssignedUserFilter = assignedUserFilter,
                DateFilter = dateFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Activities/Create
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            await PopulateFormDropdownsAsync(customerId, leadId);
            var model = new ActivityCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow,
                ActivityType = "Call",
                Status = "Completed",
                AssignedTo = _userScopeService.GetCurrentUserId()
            };
            return View(model);
        }

        // POST: Activities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var activity = new Activity
                {
                    ActivityType = model.ActivityType,
                    Subject = model.Subject.Trim(),
                    Description = model.Description?.Trim(),
                    ActivityDate = model.ActivityDate,
                    CustomerId = model.CustomerId,
                    LeadId = model.LeadId,
                    Status = model.Status,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = _userScopeService.IsSalesExecutive() ? _userScopeService.GetCurrentUserId() : model.AssignedTo
                };

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Create",
                    entityName: "Activity",
                    recordId: activity.ActivityId.ToString(),
                    newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}",
                    result: "Success",
                    details: $"Activity '{activity.Subject}' recorded.");

                TempData["Success"] = $"Activity '{activity.Subject}' logged successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateFormDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        // GET: Activities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = _userScopeService.ApplyActivityScope(_context.Activities.AsQueryable());
            var activity = await query.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null) return NotFound();

            await PopulateFormDropdownsAsync(activity.CustomerId, activity.LeadId, activity.AssignedTo);
            var model = new ActivityCreateEditViewModel
            {
                ActivityId = activity.ActivityId,
                ActivityType = activity.ActivityType,
                Subject = activity.Subject,
                Description = activity.Description,
                ActivityDate = activity.ActivityDate,
                CustomerId = activity.CustomerId,
                LeadId = activity.LeadId,
                Status = activity.Status,
                AssignedTo = activity.AssignedTo
            };
            return View(model);
        }

        // POST: Activities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel model)
        {
            if (id != model.ActivityId) return NotFound();

            var query = _userScopeService.ApplyActivityScope(_context.Activities.AsQueryable());
            var activity = await query.FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null) return NotFound();

            if (ModelState.IsValid)
            {
                string oldVal = $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}";
                activity.ActivityType = model.ActivityType;
                activity.Subject = model.Subject.Trim();
                activity.Description = model.Description?.Trim();
                activity.ActivityDate = model.ActivityDate;
                activity.CustomerId = model.CustomerId;
                activity.LeadId = model.LeadId;
                activity.Status = model.Status;
                if (!_userScopeService.IsSalesExecutive())
                {
                    activity.AssignedTo = model.AssignedTo;
                }

                _context.Update(activity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Update",
                    entityName: "Activity",
                    recordId: activity.ActivityId.ToString(),
                    oldValue: oldVal,
                    newValue: $"Type: {activity.ActivityType}, Subject: {activity.Subject}, Status: {activity.Status}",
                    result: "Success",
                    details: $"Activity '{activity.Subject}' updated.");

                TempData["Success"] = $"Activity '{activity.Subject}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateFormDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        // POST: Activities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = _userScopeService.ApplyActivityScope(_context.Activities.AsQueryable());
            var activity = await query.FirstOrDefaultAsync(a => a.ActivityId == id);

            if (activity == null) return NotFound();

            string subject = activity.Subject;
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Delete",
                entityName: "Activity",
                recordId: id.ToString(),
                result: "Success",
                details: $"Activity '{subject}' deleted.");

            TempData["Success"] = $"Activity '{subject}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateFormDropdownsAsync(int? customerId = null, int? leadId = null, string? assignedUserId = null)
        {
            var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserId);
        }
    }
}
