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
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
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

        // GET: Leads
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? companyFilter,
            string? statusFilter,
            string? priorityFilter,
            string? assignedUserFilter,
            int page = 1)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .Include(l => l.ConvertedCustomer)
                .AsQueryable();

            query = _userScopeService.ApplyLeadScope(query);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                                         l.Email.ToLower().Contains(term) ||
                                         l.Phone.Contains(term) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(companyFilter))
            {
                query = query.Where(l => l.CompanyName != null && l.CompanyName.Contains(companyFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
            {
                query = query.Where(l => l.Priority == priorityFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedUserFilter))
            {
                query = query.Where(l => l.AssignedTo == assignedUserFilter);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedUserFilter);

            var viewModel = new LeadIndexViewModel
            {
                Leads = leads,
                SearchTerm = searchTerm,
                CompanyFilter = companyFilter,
                StatusFilter = statusFilter,
                PriorityFilter = priorityFilter,
                AssignedUserFilter = assignedUserFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Leads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .AsQueryable();

            query = _userScopeService.ApplyLeadScope(query);
            var lead = await query.FirstOrDefaultAsync(m => m.LeadId == id);

            if (lead == null) return NotFound();

            return View(lead);
        }

        // GET: Leads/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropdownAsync();
            var model = new LeadCreateEditViewModel
            {
                AssignedTo = _userScopeService.GetCurrentUserId()
            };
            return View(model);
        }

        // POST: Leads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadCreateEditViewModel model)
        {
            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError("ExpectedValue", "Expected value must be 0 or a positive number.");
            }

            if (ModelState.IsValid)
            {
                int nextId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 2000) + 1;
                string leadCode = $"LEAD-{nextId}";

                var lead = new Lead
                {
                    LeadCode = leadCode,
                    LeadName = model.LeadName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone.Trim(),
                    CompanyName = model.CompanyName?.Trim(),
                    Source = model.Source,
                    Status = model.Status,
                    Priority = model.Priority,
                    ExpectedValue = model.ExpectedValue,
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = _userScopeService.IsSalesExecutive() ? _userScopeService.GetCurrentUserId() : model.AssignedTo,
                    Notes = model.Notes?.Trim()
                };

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Create",
                    entityName: "Lead",
                    recordId: lead.LeadId.ToString(),
                    newValue: $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}",
                    result: "Success",
                    details: $"Lead '{lead.LeadName}' created.");

                TempData["Success"] = $"Lead '{lead.LeadName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        // GET: Leads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            var model = new LeadCreateEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                Notes = lead.Notes
            };

            await PopulateUsersDropdownAsync(lead.AssignedTo);
            return View(model);
        }

        // POST: Leads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel model)
        {
            if (id != model.LeadId) return NotFound();

            var query = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            // Business Workflow Rule: If lead was already Converted, status cannot be reverted
            if (lead.Status == "Converted" && model.Status != "Converted")
            {
                ModelState.AddModelError("Status", "A converted lead cannot be moved back to an earlier pipeline status.");
            }

            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
            }

            if (ModelState.IsValid)
            {
                string oldValue = $"Status: {lead.Status}, Value: {lead.ExpectedValue}";

                lead.LeadName = model.LeadName.Trim();
                lead.Email = model.Email.Trim().ToLower();
                lead.Phone = model.Phone.Trim();
                lead.CompanyName = model.CompanyName?.Trim();
                lead.Source = model.Source;
                lead.Status = model.Status;
                lead.Priority = model.Priority;
                lead.ExpectedValue = model.ExpectedValue;
                lead.Notes = model.Notes?.Trim();

                if (!_userScopeService.IsSalesExecutive() && !string.IsNullOrEmpty(model.AssignedTo))
                {
                    lead.AssignedTo = model.AssignedTo;
                }

                _context.Update(lead);
                await _context.SaveChangesAsync();

                string newValue = $"Status: {lead.Status}, Value: {lead.ExpectedValue}";

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Update",
                    entityName: "Lead",
                    recordId: lead.LeadId.ToString(),
                    oldValue: oldValue,
                    newValue: newValue,
                    result: "Success",
                    details: $"Lead '{lead.LeadName}' updated.");

                TempData["Success"] = $"Lead '{lead.LeadName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        // GET: Leads/Convert/5
        public async Task<IActionResult> Convert(int? id)
        {
            if (id == null) return NotFound();

            var query = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["Warning"] = "This lead has already been converted into a customer.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            var model = new ConvertLeadViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = $"{lead.LeadName} - Expansion Deal",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 100000m,
                OpportunityProbability = 30,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                AssignedTo = lead.AssignedTo
            };

            await PopulateUsersDropdownAsync(lead.AssignedTo);
            return View(model);
        }

        // POST: Leads/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(ConvertLeadViewModel model)
        {
            var query = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == model.LeadId);

            if (lead == null) return NotFound();

            if (lead.Status == "Converted")
            {
                TempData["Warning"] = "This lead has already been converted.";
                return RedirectToAction(nameof(Index));
            }

            // Check if customer with this email or phone already exists
            Customer? customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == model.Email.ToLower());
            if (customer == null)
            {
                int nextCustId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;
                customer = new Customer
                {
                    CustomerCode = $"CUST-{nextCustId}",
                    CustomerName = model.CustomerName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone.Trim(),
                    CompanyName = model.CompanyName?.Trim(),
                    Address = model.Address?.Trim(),
                    City = model.City?.Trim(),
                    State = model.State?.Trim(),
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = lead.AssignedTo ?? _userScopeService.GetCurrentUserId()
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Optionally create opportunity
            Opportunity? opp = null;
            if (model.CreateOpportunity && model.OpportunityAmount.HasValue && model.OpportunityAmount.Value > 0)
            {
                opp = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} Deal" : model.OpportunityName.Trim(),
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount.Value,
                    Stage = "Qualification",
                    Probability = model.OpportunityProbability,
                    ExpectedCloseDate = model.ExpectedCloseDate < DateTime.Today ? DateTime.Today.AddDays(30) : model.ExpectedCloseDate,
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = lead.AssignedTo ?? _userScopeService.GetCurrentUserId(),
                    Notes = $"Converted from Lead {lead.LeadCode}"
                };
                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
            }

            // Mark lead as Converted
            lead.Status = "Converted";
            lead.ConvertedCustomerId = customer.CustomerId;
            _context.Update(lead);
            await _context.SaveChangesAsync();

            // Audit conversion
            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Update",
                entityName: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: "Status: " + lead.Status,
                newValue: $"Status: Converted, CustomerId: {customer.CustomerId}, OppId: {opp?.OpportunityId}",
                result: "Success",
                details: $"Lead '{lead.LeadName}' converted to Customer '{customer.CustomerName}'." + (opp != null ? $" Created Opportunity '{opp.OpportunityName}'." : ""));

            TempData["Success"] = $"Lead '{lead.LeadName}' was successfully converted into Customer '{customer.CustomerName}'!";
            return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
        }

        // POST: Leads/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null) return NotFound();

            string leadName = lead.LeadName;
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Delete",
                entityName: "Lead",
                recordId: id.ToString(),
                result: "Success",
                details: $"Lead '{leadName}' deleted.");

            TempData["Success"] = $"Lead '{leadName}' was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
