using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    [Produces("application/json")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;

        public ReportsApiController(
            ApplicationDbContext context,
            IUserScopeService userScopeService)
        {
            _context = context;
            _userScopeService = userScopeService;
        }

        /// <summary>
        /// Retrieves pipeline report data including stage totals and weighted revenue
        /// </summary>
        [HttpGet("pipeline")]
        [ProducesResponseType(typeof(PipelineReportApiDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPipelineReport()
        {
            var query = _context.Opportunities.AsQueryable();
            query = _userScopeService.ApplyOpportunityScope(query);

            var opps = await query.ToListAsync();

            decimal totalPipeline = opps.Where(o => o.Status == "Open").Sum(o => o.Amount);
            decimal totalWeighted = opps.Where(o => o.Status == "Open").Sum(o => o.WeightedAmount);

            var stages = opps
                .GroupBy(o => o.Stage)
                .Select(g => new StageSummaryDto
                {
                    Stage = g.Key,
                    DealCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => x.WeightedAmount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            var result = new PipelineReportApiDto
            {
                TotalPipelineValue = totalPipeline,
                TotalWeightedPipelineValue = totalWeighted,
                TotalDealsCount = opps.Count,
                StageBreakdown = stages
            };

            return Ok(result);
        }
    }
}
