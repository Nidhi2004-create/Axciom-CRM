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
    [Route("api/opportunities")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    [Produces("application/json")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IAuditService _auditService;

        public OpportunitiesApiController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IAuditService auditService)
        {
            _context = context;
            _userScopeService = userScopeService;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves scoped opportunities
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<OpportunityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? stage, [FromQuery] string? status)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            query = _userScopeService.ApplyOpportunityScope(query);

            if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

            var opps = await query
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new OpportunityDto
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : "—",
                    LeadId = o.LeadId,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    WeightedAmount = Math.Round((o.Amount * o.Probability) / 100m, 2),
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    CreatedDate = o.CreatedDate,
                    OwnerName = o.AssignedToUser != null ? o.AssignedToUser.FullName : "Unassigned"
                })
                .ToListAsync();

            return Ok(opps);
        }

        /// <summary>
        /// Creates a new sales opportunity
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OpportunityDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Mandatory Business Validation: Amount > 0
            if (dto.Amount <= 0)
            {
                return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
            }

            // Mandatory Business Validation: Probability 0-100
            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(new { message = "Probability must be between 0 and 100." });
            }

            // Mandatory Business Validation: Close date cannot be in the past
            if (dto.Stage != "Won" && dto.Stage != "Lost" && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Expected Close Date cannot be in the past." });
            }

            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
            {
                return BadRequest(new { message = $"Customer with ID {dto.CustomerId} does not exist." });
            }

            string status = "Open";
            if (dto.Stage == "Won") { status = "Won"; dto.Probability = 100; }
            else if (dto.Stage == "Lost") { status = "Lost"; dto.Probability = 0; }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName.Trim(),
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = status,
                Notes = dto.Notes?.Trim(),
                CreatedDate = DateTime.UtcNow,
                AssignedTo = _userScopeService.GetCurrentUserId()
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                _userScopeService.GetCurrentUserId(),
                _userScopeService.GetCurrentUserName(),
                "Create",
                "Opportunity",
                opp.OpportunityId.ToString(),
                result: "Success",
                details: $"Opportunity '{opp.OpportunityName}' created via REST API.");

            var result = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                CustomerName = customer.CustomerName,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedAmount = opp.WeightedAmount,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                OwnerName = _userScopeService.GetCurrentUserName()
            };

            return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, result);
        }
    }
}
