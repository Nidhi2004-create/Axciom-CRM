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
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
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

        // GET: FollowUps
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter,
            string? typeFilter,
            string? assignedUserFilter,
            DateTime? dateFilter,
            int page = 1)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedToUser)
                .AsQueryable();

            query = _userScopeService.ApplyFollowUpScope(query);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter == "Overdue")
                {
                    query = query.Where(f => f.Status == "Planned" && f.FollowUpDate < DateTime.UtcNow);
                }
                else
                {
                    query = query.Where(f => f.Status == statusFilter);
                }
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(f => f.AssignedTo == assignedUserFilter);
            }

            if (dateFilter.HasValue)
            {
                var d = dateFilter.Value.Date;
                query = query.Where(f => f.FollowUpDate.Date == d);
            }

            var allItems = await query.ToListAsync();
            int overdueCount = allItems.Count(f => f.Status == "Planned" && f.FollowUpDate < DateTime.UtcNow);
            int upcomingCount = allItems.Count(f => f.Status == "Planned" && f.FollowUpDate >= DateTime.UtcNow);
            int completedCount = allItems.Count(f => f.Status == "Completed");

            int pageSize = 10;
            int totalItems = allItems.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pagedItems = allItems
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserFilter);

            var viewModel = new FollowUpIndexViewModel
            {
                FollowUps = pagedItems,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                AssignedUserFilter = assignedUserFilter,
                DateFilter = dateFilter,
                OverdueCount = overdueCount,
                UpcomingCount = upcomingCount,
                CompletedCount = completedCount,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: FollowUps/Create
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            await PopulateFormDropdownsAsync(customerId, leadId, opportunityId);
            var model = new FollowUpCreateEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.Today.AddDays(1).AddHours(10),
                FollowUpType = "Call",
                Status = "Planned",
                AssignedTo = _userScopeService.GetCurrentUserId()
            };
            return View(model);
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
        {
            // Business Validation: Follow-up date cannot be earlier than today for a new/planned follow-up
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (ModelState.IsValid)
            {
                var followUp = new FollowUp
                {
                    CustomerId = model.CustomerId,
                    LeadId = model.LeadId,
                    OpportunityId = model.OpportunityId,
                    Subject = model.Subject.Trim(),
                    FollowUpDate = model.FollowUpDate,
                    FollowUpType = model.FollowUpType,
                    Remarks = model.Remarks?.Trim(),
                    Status = model.Status,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = _userScopeService.IsSalesExecutive() ? _userScopeService.GetCurrentUserId() : model.AssignedTo
                };

                _context.FollowUps.Add(followUp);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Create",
                    entityName: "FollowUp",
                    recordId: followUp.FollowUpId.ToString(),
                    newValue: $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate}, Type: {followUp.FollowUpType}",
                    result: "Success",
                    details: $"Follow-Up '{followUp.Subject}' scheduled for {followUp.FollowUpDate:dd MMM yyyy}.");

                TempData["Success"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateFormDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        // POST: FollowUps/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionNotes)
        {
            var query = _userScopeService.ApplyFollowUpScope(_context.FollowUps.AsQueryable());
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return NotFound();

            followUp.Status = "Completed";
            followUp.CompletedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(completionNotes))
            {
                followUp.Remarks = (followUp.Remarks != null ? followUp.Remarks + "\n" : "") + $"Completed notes: {completionNotes}";
            }

            // Also create an Activity record automatically!
            var activity = new Activity
            {
                ActivityType = followUp.FollowUpType,
                Subject = $"Completed Follow-Up: {followUp.Subject}",
                Description = completionNotes ?? followUp.Remarks,
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                AssignedTo = followUp.AssignedTo,
                Status = "Completed",
                CreatedDate = DateTime.UtcNow
            };
            _context.Activities.Add(activity);

            _context.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Update",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: "Status: Planned",
                newValue: "Status: Completed",
                result: "Success",
                details: $"Follow-up '{followUp.Subject}' marked completed.");

            TempData["Success"] = $"Follow-up '{followUp.Subject}' completed!";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? reason)
        {
            var query = _userScopeService.ApplyFollowUpScope(_context.FollowUps.AsQueryable());
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return NotFound();

            if (newDate.Date < DateTime.Today)
            {
                TempData["Error"] = "Rescheduled follow-up date cannot be earlier than today.";
                return RedirectToAction(nameof(Index));
            }

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(reason))
            {
                followUp.Remarks = (followUp.Remarks != null ? followUp.Remarks + "\n" : "") + $"Rescheduled from {oldDate:dd MMM yyyy}: {reason}";
            }

            _context.Update(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Update",
                entityName: "FollowUp",
                recordId: id.ToString(),
                oldValue: $"Date: {oldDate}",
                newValue: $"Date: {newDate}",
                result: "Success",
                details: $"Follow-up '{followUp.Subject}' rescheduled from {oldDate:dd MMM yyyy} to {newDate:dd MMM yyyy}.");

            TempData["Success"] = $"Follow-up rescheduled to {newDate:dd MMM yyyy hh:mm tt}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: FollowUps/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = _userScopeService.ApplyFollowUpScope(_context.FollowUps.AsQueryable());
            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);

            if (followUp == null) return NotFound();

            string subject = followUp.Subject;
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Delete",
                entityName: "FollowUp",
                recordId: id.ToString(),
                result: "Success",
                details: $"Follow-up '{subject}' deleted.");

            TempData["Success"] = $"Follow-up '{subject}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateFormDropdownsAsync(int? customerId = null, int? leadId = null, int? opportunityId = null, string? assignedUserId = null)
        {
            var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

            var opps = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
            ViewBag.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName", opportunityId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserId);
        }
    }
}
