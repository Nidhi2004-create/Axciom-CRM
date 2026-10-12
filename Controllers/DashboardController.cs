using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userScopeService = userScopeService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null)
        {
            // Determine date boundary
            DateTime? filterStart = null;
            DateTime? filterEnd = null;
            DateTime now = DateTime.UtcNow;

            switch (dateFilter)
            {
                case "Today":
                    filterStart = DateTime.Today;
                    filterEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "ThisWeek":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    filterStart = now.AddDays(-1 * diff).Date;
                    filterEnd = filterStart.Value.AddDays(7).AddTicks(-1);
                    break;
                case "ThisMonth":
                    filterStart = new DateTime(now.Year, now.Month, 1);
                    filterEnd = filterStart.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "Custom":
                    if (startDate.HasValue) filterStart = startDate.Value.Date;
                    if (endDate.HasValue) filterEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    break;
                case "All":
                default:
                    dateFilter = "All";
                    break;
            }

            // Scoped base queries
            var customerQuery = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var leadQuery = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var oppQuery = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());
            var followUpQuery = _userScopeService.ApplyFollowUpScope(_context.FollowUps.AsQueryable());
            var activityQuery = _userScopeService.ApplyActivityScope(_context.Activities.AsQueryable());

            // Apply date filters where applicable
            if (filterStart.HasValue)
            {
                customerQuery = customerQuery.Where(c => c.CreatedDate >= filterStart.Value);
                leadQuery = leadQuery.Where(l => l.CreatedDate >= filterStart.Value);
                oppQuery = oppQuery.Where(o => o.CreatedDate >= filterStart.Value);
            }
            if (filterEnd.HasValue)
            {
                customerQuery = customerQuery.Where(c => c.CreatedDate <= filterEnd.Value);
                leadQuery = leadQuery.Where(l => l.CreatedDate <= filterEnd.Value);
                oppQuery = oppQuery.Where(o => o.CreatedDate <= filterEnd.Value);
            }

            var customersList = await customerQuery.ToListAsync();
            var leadsList = await leadQuery.ToListAsync();
            var oppsList = await oppQuery.ToListAsync();
            var followUpsList = await followUpQuery.ToListAsync();
            var activitiesList = await activityQuery.ToListAsync();

            // KPIs
            int totalCustomers = customersList.Count;
            int totalLeads = leadsList.Count;
            int openLeads = leadsList.Count(l => l.Status != "Lost" && l.Status != "Converted");
            int totalOpps = oppsList.Count;
            int openOpps = oppsList.Count(o => o.Status == "Open");
            int wonOpps = oppsList.Count(o => o.Status == "Won");
            int lostOpps = oppsList.Count(o => o.Status == "Lost");
            decimal totalPipeline = oppsList.Where(o => o.Status == "Open").Sum(o => o.Amount);
            decimal totalWeighted = oppsList.Where(o => o.Status == "Open").Sum(o => o.WeightedAmount);

            int pendingFollowUps = followUpsList.Count(f => f.Status == "Planned");
            int overdueFollowUps = followUpsList.Count(f => f.Status == "Planned" && f.FollowUpDate < DateTime.UtcNow);

            // User & Role identity
            string role = _userScopeService.IsAdmin() ? "Admin" : (_userScopeService.IsManager() ? "Manager" : "SalesExecutive");
            var currentUser = await _userManager.GetUserAsync(User);

            int totalUsers = _userScopeService.IsAdmin() ? await _userManager.Users.CountAsync() : 0;
            int auditEvents = (_userScopeService.IsAdmin() || _userScopeService.IsManager()) ? await _context.AuditLogs.CountAsync() : 0;

            // Chart 1: Lead Status Breakdown
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
            var leadStatusCounts = leadStatuses.Select(s => leadsList.Count(l => l.Status == s)).ToList();

            // Chart 2: Opportunity Pipeline by Stage
            var pipelineStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var pipelineAmounts = pipelineStages.Select(st => oppsList.Where(o => o.Stage == st).Sum(o => o.Amount)).ToList();

            // Chart 3: Monthly Sales / Won deals (last 6 months)
            var monthlyLabels = new List<string>();
            var monthlyAmounts = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = DateTime.Today.AddMonths(-i);
                var monthLabel = targetMonth.ToString("MMM yyyy");
                monthlyLabels.Add(monthLabel);

                var monthWonTotal = oppsList
                    .Where(o => o.Status == "Won" && o.CreatedDate.Year == targetMonth.Year && o.CreatedDate.Month == targetMonth.Month)
                    .Sum(o => o.Amount);
                monthlyAmounts.Add(monthWonTotal);
            }

            // Recent Workbench
            var upcoming = followUpsList
                .Where(f => f.Status == "Planned")
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToList();

            var recentActs = activitiesList
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToList();

            var viewModel = new DashboardViewModel
            {
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                OpenLeads = openLeads,
                TotalOpportunities = totalOpps,
                OpenOpportunities = openOpps,
                WonOpportunities = wonOpps,
                LostOpportunities = lostOpps,
                TotalPipelineValue = totalPipeline,
                TotalWeightedPipelineValue = totalWeighted,
                PendingFollowUpsCount = pendingFollowUps,
                OverdueFollowUpsCount = overdueFollowUps,
                TotalUsersCount = totalUsers,
                TotalAuditEventsCount = auditEvents,
                UserRole = role,
                UserFullName = currentUser?.FullName ?? User.Identity?.Name ?? "User",
                DateFilter = dateFilter,
                CustomStartDate = startDate,
                CustomEndDate = endDate,
                LeadStatusLabels = leadStatuses.ToList(),
                LeadStatusCounts = leadStatusCounts,
                PipelineStageLabels = pipelineStages.ToList(),
                PipelineStageAmounts = pipelineAmounts,
                MonthlySalesLabels = monthlyLabels,
                MonthlySalesAmounts = monthlyAmounts,
                UpcomingFollowUps = upcoming,
                RecentActivities = recentActs
            };

            return View(viewModel);
        }
    }
}
