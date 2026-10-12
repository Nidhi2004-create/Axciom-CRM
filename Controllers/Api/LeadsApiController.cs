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
    [Route("api/leads")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    [Produces("application/json")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public LeadsApiController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves scoped leads
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<LeadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status)
        {
            var query = _context.Leads.Include(l => l.AssignedToUser).AsQueryable();
            query = _userScopeService.ApplyLeadScope(query);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                                         l.Email.ToLower().Contains(term) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new LeadDto
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    CreatedDate = l.CreatedDate,
                    AssignedUserName = l.AssignedToUser != null ? l.AssignedToUser.FullName : "Unassigned"
                })
                .ToListAsync();

            return Ok(leads);
        }

        /// <summary>
        /// Creates a new prospective lead
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(LeadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLead([FromBody] LeadCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.ExpectedValue < 0)
            {
                return BadRequest(new { message = "Expected deal value cannot be negative." });
            }

            int nextId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 2000) + 1;
            var lead = new Lead
            {
                LeadCode = $"LEAD-{nextId}",
                LeadName = dto.LeadName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone.Trim(),
                CompanyName = dto.CompanyName?.Trim(),
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                Notes = dto.Notes?.Trim(),
                CreatedDate = DateTime.UtcNow,
                AssignedTo = _userScopeService.GetCurrentUserId()
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                result: "Success",
                details: $"Lead '{lead.LeadName}' created via REST API.");

            var result = new LeadDto
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
                CreatedDate = lead.CreatedDate,
                AssignedUserName = _userScopeService.GetCurrentUserName()
            };

            return CreatedAtAction(nameof(GetLeads), new { id = lead.LeadId }, result);
        }
    }
}
