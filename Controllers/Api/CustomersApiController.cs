using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/customers")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    [Produces("application/json")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public CustomersApiController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves scoped list of customers
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search)
        {
            var query = _context.Customers.Include(c => c.CreatedByUser).AsQueryable();
            query = _userScopeService.ApplyCustomerScope(query);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                                         c.Email.ToLower().Contains(term) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Status = c.Status,
                    CreatedDate = c.CreatedDate,
                    OwnerName = c.CreatedByUser != null ? c.CreatedByUser.FullName : "Unassigned"
                })
                .ToListAsync();

            return Ok(customers);
        }

        /// <summary>
        /// Retrieves a customer record by ID
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var query = _context.Customers.Include(c => c.CreatedByUser).AsQueryable();
            query = _userScopeService.ApplyCustomerScope(query);

            var c = await query.FirstOrDefaultAsync(x => x.CustomerId == id);
            if (c == null) return NotFound(new { message = $"Customer with ID {id} not found." });

            return Ok(new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerName = c.CreatedByUser?.FullName ?? "Unassigned"
            });
        }

        /// <summary>
        /// Creates a new customer
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Validation: Email Uniqueness
            if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
            {
                return Conflict(new { message = "A customer with this email address already exists." });
            }

            // Validation: Phone Uniqueness
            if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
            {
                return Conflict(new { message = "A customer with this phone number already exists." });
            }

            int nextId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;
            var customer = new Customer
            {
                CustomerCode = $"CUST-{nextId}",
                CustomerName = dto.CustomerName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                Status = dto.Status,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = _userScopeService.GetCurrentUserId()
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                result: "Success",
                details: $"Customer '{customer.CustomerName}' created via REST API.");

            var resultDto = new CustomerDto
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
                CreatedDate = customer.CreatedDate,
                OwnerName = _userScopeService.GetCurrentUserName()
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
        }

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var query = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == dto.Email.ToLower()))
            {
                return Conflict(new { message = "Another customer already uses this email address." });
            }

            if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == dto.Phone))
            {
                return Conflict(new { message = "Another customer already uses this phone number." });
            }

            customer.CustomerName = dto.CustomerName.Trim();
            customer.Email = dto.Email.Trim().ToLower();
            customer.Phone = dto.Phone.Trim();
            customer.CompanyName = dto.CompanyName?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.City = dto.City?.Trim();
            customer.State = dto.State?.Trim();
            customer.Status = dto.Status;
            customer.ModifiedDate = DateTime.UtcNow;

            _context.Update(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Update",
                "Customer",
                customer.CustomerId.ToString(),
                result: "Success",
                details: $"Customer '{customer.CustomerName}' updated via REST API.");

            return Ok(new CustomerDto
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
                CreatedDate = customer.CreatedDate,
                OwnerName = _userScopeService.GetCurrentUserName()
            });
        }

        /// <summary>
        /// Deletes or deactivates a customer
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var query = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

            string name = customer.CustomerName;
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Delete",
                "Customer",
                id.ToString(),
                result: "Success",
                details: $"Customer '{name}' deleted via REST API.");

            return Ok(new { message = $"Customer '{name}' was successfully deleted." });
        }
    }
}
