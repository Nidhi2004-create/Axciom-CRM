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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
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

        // GET: Customers
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? emailFilter,
            string? phoneFilter,
            string? companyFilter,
            string? statusFilter,
            int page = 1)
        {
            var query = _context.Customers.Include(c => c.CreatedByUser).AsQueryable();

            // Apply Role-based Scoping
            query = _userScopeService.ApplyCustomerScope(query);

            // Filtering & Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                                         c.Email.ToLower().Contains(term) ||
                                         c.Phone.Contains(term) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                query = query.Where(c => c.Email.Contains(emailFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(phoneFilter))
            {
                query = query.Where(c => c.Phone.Contains(phoneFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(companyFilter))
            {
                query = query.Where(c => c.CompanyName != null && c.CompanyName.Contains(companyFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CustomerIndexViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm,
                EmailFilter = emailFilter,
                PhoneFilter = phoneFilter,
                CompanyFilter = companyFilter,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var query = _context.Customers
                .Include(c => c.CreatedByUser)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .AsQueryable();

            query = _userScopeService.ApplyCustomerScope(query);
            var customer = await query.FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            var viewModel = new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
                FollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
                Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
            };

            return View(viewModel);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropdownAsync();
            var model = new CustomerCreateEditViewModel
            {
                CreatedBy = _userScopeService.GetCurrentUserId()
            };
            return View(model);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
        {
            // Business Validation 1: Email Uniqueness
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            // Business Validation 2: Phone Uniqueness
            if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            // Business Validation 3: Duplicate Customer (Same Name + Company)
            if (!string.IsNullOrWhiteSpace(model.CompanyName) &&
                await _context.Customers.AnyAsync(c => c.CustomerName.ToLower() == model.CustomerName.ToLower() &&
                                                       c.CompanyName != null && c.CompanyName.ToLower() == model.CompanyName.ToLower()))
            {
                ModelState.AddModelError("CustomerName", "A customer with this name and company already exists.");
            }

            if (ModelState.IsValid)
            {
                // Generate CustomerCode
                int nextId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;
                string customerCode = $"CUST-{nextId}";

                var customer = new Customer
                {
                    CustomerCode = customerCode,
                    CustomerName = model.CustomerName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone.Trim(),
                    CompanyName = model.CompanyName?.Trim(),
                    Address = model.Address?.Trim(),
                    City = model.City?.Trim(),
                    State = model.State?.Trim(),
                    Status = model.Status,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _userScopeService.IsSalesExecutive() ? _userScopeService.GetCurrentUserId() : model.CreatedBy
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Create",
                    entityName: "Customer",
                    recordId: customer.CustomerId.ToString(),
                    newValue: $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}",
                    result: "Success",
                    details: $"Customer '{customer.CustomerName}' created.");

                TempData["Success"] = $"Customer '{customer.CustomerName}' was successfully created.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropdownAsync(model.CreatedBy);
            return View(model);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var query = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            var model = new CustomerCreateEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedBy = customer.CreatedBy
            };

            await PopulateUsersDropdownAsync(customer.CreatedBy);
            return View(model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel model)
        {
            if (id != model.CustomerId) return NotFound();

            var query = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            // Email uniqueness among OTHER customers
            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == model.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Another customer with this email address already exists.");
            }

            // Phone uniqueness among OTHER customers
            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == model.Phone))
            {
                ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
            }

            if (ModelState.IsValid)
            {
                string oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

                customer.CustomerName = model.CustomerName.Trim();
                customer.Email = model.Email.Trim().ToLower();
                customer.Phone = model.Phone.Trim();
                customer.CompanyName = model.CompanyName?.Trim();
                customer.Address = model.Address?.Trim();
                customer.City = model.City?.Trim();
                customer.State = model.State?.Trim();
                customer.Status = model.Status;
                customer.ModifiedDate = DateTime.UtcNow;

                if (!_userScopeService.IsSalesExecutive() && !string.IsNullOrEmpty(model.CreatedBy))
                {
                    customer.CreatedBy = model.CreatedBy;
                }

                _context.Update(customer);
                await _context.SaveChangesAsync();

                string newValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

                await _auditService.LogAsync(
                    userId: _userScopeService.GetCurrentUserId(),
                    userName: _userScopeService.GetCurrentUserName(),
                    action: "Update",
                    entityName: "Customer",
                    recordId: customer.CustomerId.ToString(),
                    oldValue: oldValue,
                    newValue: newValue,
                    result: "Success",
                    details: $"Customer '{customer.CustomerName}' updated.");

                TempData["Success"] = $"Customer '{customer.CustomerName}' was updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropdownAsync(model.CreatedBy);
            return View(model);
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var query = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            // Safe deactivation or deletion
            string customerName = customer.CustomerName;
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId: _userScopeService.GetCurrentUserId(),
                userName: _userScopeService.GetCurrentUserName(),
                action: "Delete",
                entityName: "Customer",
                recordId: id.ToString(),
                result: "Success",
                details: $"Customer '{customerName}' was deleted.");

            TempData["Success"] = $"Customer '{customerName}' was successfully deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(string? selectedId = null)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
        }
    }
}
