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
    [Route("api/followups")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    [Produces("application/json")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public FollowUpsApiController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves scoped follow-ups
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<FollowUpDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? status, [FromQuery] string? type)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .AsQueryable();

            query = _userScopeService.ApplyFollowUpScope(query);

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(f => f.FollowUpType == type);

            var list = await query
                .OrderBy(f => f.FollowUpDate)
                .Select(f => new FollowUpDto
                {
                    FollowUpId = f.FollowUpId,
                    Subject = f.Subject,
                    FollowUpType = f.FollowUpType,
                    FollowUpDate = f.FollowUpDate,
                    Status = f.Status,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                    LeadId = f.LeadId,
                    LeadName = f.Lead != null ? f.Lead.LeadName : null,
                    AssignedUserName = f.AssignedToUser != null ? f.AssignedToUser.FullName : null,
                    Remarks = f.Remarks
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>
        /// Creates a scheduled follow-up
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(FollowUpDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Business Validation: Follow-up date cannot be earlier than today
            if (dto.FollowUpDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
            }

            var followUp = new FollowUp
            {
                Subject = dto.Subject.Trim(),
                FollowUpType = dto.FollowUpType,
                FollowUpDate = dto.FollowUpDate,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                OpportunityId = dto.OpportunityId,
                Remarks = dto.Remarks?.Trim(),
                Status = "Planned",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = _userScopeService.GetCurrentUserId()
            };

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                result: "Success",
                details: $"Follow-Up '{followUp.Subject}' created via REST API.");

            var result = new FollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                Subject = followUp.Subject,
                FollowUpType = followUp.FollowUpType,
                FollowUpDate = followUp.FollowUpDate,
                Status = followUp.Status,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                Remarks = followUp.Remarks,
                AssignedUserName = _userScopeService.GetCurrentUserName()
            };

            return CreatedAtAction(nameof(GetFollowUps), new { id = followUp.FollowUpId }, result);
        }
    }
}
