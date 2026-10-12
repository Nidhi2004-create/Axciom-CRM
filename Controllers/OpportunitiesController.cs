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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
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

        // GET: Opportunities
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? customerFilter,
            string? stageFilter,
            string? statusFilter,
            string? assignedUserFilter,
            int page = 1)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            query = _userScopeService.ApplyOpportunityScope(query);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                         o.Customer!.CustomerName.ToLower().Contains(term));
            }

            if (customerFilter.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(o => o.AssignedTo == assignedUserFilter);
            }

            // Pipeline metrics across filtered dataset
            var allFiltered = await query.ToListAsync();
            decimal totalPipeline = allFiltered.Where(o => o.Status == "Open").Sum(o => o.Amount);
            decimal totalWeighted = allFiltered.Where(o => o.Status == "Open").Sum(o => o.WeightedAmount);

            int pageSize = 10;
            int totalItems = allFiltered.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pagedItems = allFiltered
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            await PopulateFilterDropdownsAsync(customerFilter, assignedUserFilter);

            var viewModel = new OpportunityIndexViewModel
            {
                Opportunities = pagedItems,
                SearchTerm = searchTerm,
                CustomerFilter = customerFilter,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                AssignedUserFilter = assignedUserFilter,
                TotalPipelineValue = totalPipeline,
                TotalWeightedValue = totalWeighted,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Opportunities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .Include(o => o.FollowUps)
                .AsQueryable();

            query = _userScopeService.ApplyOpportunityScope(query);
            var opportunity = await query.FirstOrDefaultAsync(m => m.OpportunityId == id);

            if (opportunity == null) return NotFound();

            return View(opportunity);
        }

        // GET: Opportunities/Create
        public async Task<IActionResult> Create(int? customerId = null)
        {
            await PopulateFormDropdownsAsync(customerId);
            var model = new OpportunityCreateEditViewModel
            {
                CustomerId = customerId ?? 0,
                AssignedTo = _userScopeService.GetCurrentUserId(),
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Probability = 20,
                Stage = "Qualification"
            };
            return View(model);
        }

        // POST: Opportunities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
        {
            // Business Validation 1: Amount must be greater than 0
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            // Business Validation 2: Probability must be between 0 and 100
            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            // Business Validation 3: Expected Close Date cannot be in the past for active opportunities
            if (model.Stage != "Won" && model.Stage != "Lost" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                // Stage to Status alignment
                string status = "Open";
                if (model.Stage == "Won")
                {
                    status = "Won";
                    model.Probability = 100;
                }
                else if (model.Stage == "Lost")
                {
                    status = "Lost";
                    model.Probability = 0;
                }

                var opportunity = new Opportunity
                {
                    OpportunityName = model.OpportunityName.Trim(),
                    CustomerId = model.CustomerId,
                    LeadId = model.LeadId,
                    Amount = model.Amount,
                    Stage = model.Stage,
                    Probability = model.Probability,
                    ExpectedCloseDate = model.ExpectedCloseDate,
                    Status = status,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = _userScopeService.IsSalesExecutive() ? _userScopeService.GetCurrentUserId() : model.AssignedTo,
                    Notes = model.Notes?.Trim()
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Create",
                    entityName: "Opportunity",
                    recordId: opportunity.OpportunityId.ToString(),
                    newValue: $"Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount}, Stage: {opportunity.Stage}",
                    result: "Success",
                    details: $"Opportunity '{opportunity.OpportunityName}' created with amount ₹{opportunity.Amount:N2}.");

                TempData["Success"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateFormDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        // GET: Opportunities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());
            var opportunity = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            var model = new OpportunityCreateEditViewModel
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityName = opportunity.OpportunityName,
                CustomerId = opportunity.CustomerId,
                LeadId = opportunity.LeadId,
                Amount = opportunity.Amount,
                Stage = opportunity.Stage,
                Probability = opportunity.Probability,
                ExpectedCloseDate = opportunity.ExpectedCloseDate,
                Status = opportunity.Status,
                AssignedTo = opportunity.AssignedTo,
                Notes = opportunity.Notes
            };

            await PopulateFormDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(model);
        }

        // POST: Opportunities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
        {
            if (id != model.OpportunityId) return NotFound();

            var query = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());
            var opportunity = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            // Business Validation 1: Amount > 0
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            // Business Validation 2: Probability 0-100
            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            // Business Validation 3: Expected Close Date cannot be in past for active opportunities
            if (model.Stage != "Won" && model.Stage != "Lost" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (ModelState.IsValid)
            {
                string oldValue = $"Stage: {opportunity.Stage}, Amount: {opportunity.Amount}, Prob: {opportunity.Probability}%";

                // Stage alignment
                string status = "Open";
                if (model.Stage == "Won")
                {
                    status = "Won";
                    model.Probability = 100;
                }
                else if (model.Stage == "Lost")
                {
                    status = "Lost";
                    model.Probability = 0;
                }

                opportunity.OpportunityName = model.OpportunityName.Trim();
                opportunity.CustomerId = model.CustomerId;
                opportunity.LeadId = model.LeadId;
                opportunity.Amount = model.Amount;
                opportunity.Stage = model.Stage;
                opportunity.Probability = model.Probability;
                opportunity.ExpectedCloseDate = model.ExpectedCloseDate;
                opportunity.Status = status;
                opportunity.Notes = model.Notes?.Trim();

                if (!_userScopeService.IsSalesExecutive() && !string.IsNullOrEmpty(model.AssignedTo))
                {
                    opportunity.AssignedTo = model.AssignedTo;
                }

                _context.Update(opportunity);
                await _context.SaveChangesAsync();

                string newValue = $"Stage: {opportunity.Stage}, Amount: {opportunity.Amount}, Prob: {opportunity.Probability}%";

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Update",
                    entityName: "Opportunity",
                    recordId: opportunity.OpportunityId.ToString(),
                    oldValue: oldValue,
                    newValue: newValue,
                    result: "Success",
                    details: $"Opportunity '{opportunity.OpportunityName}' updated to stage '{opportunity.Stage}'.");

                TempData["Success"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateFormDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        // POST: Opportunities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());
            var opportunity = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            string oppName = opportunity.OpportunityName;
            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Delete",
                entityName: "Opportunity",
                recordId: id.ToString(),
                result: "Success",
                details: $"Opportunity '{oppName}' deleted.");

            TempData["Success"] = $"Opportunity '{oppName}' was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateFilterDropdownsAsync(int? customerId, string? assignedUserId)
        {
            var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserId);
        }

        private async Task PopulateFormDropdownsAsync(int? customerId = null, int? leadId = null, string? assignedUserId = null)
        {
            var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
            ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserId);
        }
    }
}
