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
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserScopeService _userScopeService;
        private readonly IExportService _exportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            IUserScopeService userScopeService,
            IExportService exportService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userScopeService = userScopeService;
            _exportService = exportService;
            _userManager = userManager;
        }

        // GET: Reports Hub
        public async Task<IActionResult> Index()
        {
            var customerQuery = _userScopeService.ApplyCustomerScope(_context.Customers.AsQueryable());
            var leadQuery = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var oppQuery = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());
            var followUpQuery = _userScopeService.ApplyFollowUpScope(_context.FollowUps.AsQueryable());
            var actQuery = _userScopeService.ApplyActivityScope(_context.Activities.AsQueryable());

            var model = new ReportsHubViewModel
            {
                TotalCustomers = await customerQuery.CountAsync(),
                TotalLeads = await leadQuery.CountAsync(),
                ConvertedLeads = await leadQuery.CountAsync(l => l.Status == "Converted"),
                TotalPipelineValue = await oppQuery.Where(o => o.Status == "Open").SumAsync(o => o.Amount),
                TotalWeightedPipelineValue = (await oppQuery.Where(o => o.Status == "Open").ToListAsync()).Sum(o => o.WeightedAmount),
                CompletedFollowUps = await followUpQuery.CountAsync(f => f.Status == "Completed"),
                TotalActivities = await actQuery.CountAsync()
            };

            return View(model);
        }

        // 1. Customer Report
        public async Task<IActionResult> CustomerReport(string? status, DateTime? startDate, DateTime? endDate, bool export = false)
        {
            var query = _context.Customers.Include(c => c.CreatedByUser).AsQueryable();
            query = _userScopeService.ApplyCustomerScope(query);

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
            if (startDate.HasValue) query = query.Where(c => c.CreatedDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(c => c.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    data,
                    c => new[] { c.CustomerCode, c.CustomerName, c.CompanyName ?? "", c.Email, c.Phone, c.Status, c.CreatedByUser?.FullName ?? "Unassigned", c.CreatedDate.ToString("yyyy-MM-dd") },
                    new[] { "CustomerCode", "CustomerName", "Company", "Email", "Phone", "Status", "Owner", "CreatedDate" });
                return File(csv, "text/csv", $"CustomerReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Status = status;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(data);
        }

        // 2. Lead Report
        public async Task<IActionResult> LeadReport(string? status, string? source, DateTime? startDate, DateTime? endDate, bool export = false)
        {
            var query = _context.Leads.Include(l => l.AssignedToUser).AsQueryable();
            query = _userScopeService.ApplyLeadScope(query);

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
            if (!string.IsNullOrWhiteSpace(source)) query = query.Where(l => l.Source == source);
            if (startDate.HasValue) query = query.Where(l => l.CreatedDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(l => l.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    data,
                    l => new[] { l.LeadCode, l.LeadName, l.CompanyName ?? "", l.Email, l.Phone, l.Source, l.Status, l.Priority, l.ExpectedValue.ToString("F2"), l.AssignedToUser?.FullName ?? "Unassigned", l.CreatedDate.ToString("yyyy-MM-dd") },
                    new[] { "LeadCode", "LeadName", "Company", "Email", "Phone", "Source", "Status", "Priority", "ExpectedValue", "AssignedTo", "CreatedDate" });
                return File(csv, "text/csv", $"LeadReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Status = status;
            ViewBag.Source = source;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(data);
        }

        // 3. Follow-Up Report
        public async Task<IActionResult> FollowUpReport(string? status, string? type, DateTime? startDate, DateTime? endDate, bool export = false)
        {
            var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.AssignedToUser).AsQueryable();
            query = _userScopeService.ApplyFollowUpScope(query);

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(f => f.FollowUpType == type);
            if (startDate.HasValue) query = query.Where(f => f.FollowUpDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(f => f.FollowUpDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderBy(f => f.FollowUpDate).ToListAsync();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    data,
                    f => new[] { f.Subject, f.FollowUpType, f.Customer?.CustomerName ?? "", f.FollowUpDate.ToString("yyyy-MM-dd HH:mm"), f.Status, f.AssignedToUser?.FullName ?? "Unassigned", f.Remarks ?? "" },
                    new[] { "Subject", "Type", "Customer", "ScheduledDate", "Status", "AssignedTo", "Remarks" });
                return File(csv, "text/csv", $"FollowUpReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(data);
        }

        // 4. Opportunity Report
        public async Task<IActionResult> OpportunityReport(string? stage, string? status, DateTime? startDate, DateTime? endDate, bool export = false)
        {
            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedToUser).AsQueryable();
            query = _userScopeService.ApplyOpportunityScope(query);

            if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
            if (startDate.HasValue) query = query.Where(o => o.CreatedDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(o => o.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    data,
                    o => new[] { o.OpportunityName, o.Customer?.CustomerName ?? "", o.Stage, o.Status, o.Amount.ToString("F2"), $"{o.Probability}%", o.WeightedAmount.ToString("F2"), o.ExpectedCloseDate.ToString("yyyy-MM-dd"), o.AssignedToUser?.FullName ?? "Unassigned" },
                    new[] { "OpportunityName", "Customer", "Stage", "Status", "Amount", "Probability", "WeightedAmount", "ExpectedCloseDate", "Owner" });
                return File(csv, "text/csv", $"OpportunityReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Stage = stage;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(data);
        }

        // 5. Pipeline Report (Stage-wise & Owner-wise)
        public async Task<IActionResult> PipelineReport(bool export = false)
        {
            var query = _userScopeService.ApplyOpportunityScope(_context.Opportunities.Include(o => o.AssignedToUser).AsQueryable());
            var opps = await query.ToListAsync();

            var stageSummaries = opps
                .GroupBy(o => o.Stage)
                .Select(g => new StagePipelineSummary
                {
                    Stage = g.Key,
                    DealCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => x.WeightedAmount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            var ownerSummaries = opps
                .GroupBy(o => o.AssignedToUser?.FullName ?? "Unassigned")
                .Select(g => new OwnerPipelineSummary
                {
                    OwnerName = g.Key,
                    DealCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => x.WeightedAmount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    stageSummaries,
                    s => new[] { s.Stage, s.DealCount.ToString(), s.TotalAmount.ToString("F2"), s.WeightedAmount.ToString("F2") },
                    new[] { "Stage", "DealCount", "TotalAmount", "WeightedAmount" });
                return File(csv, "text/csv", $"PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.OwnerSummaries = ownerSummaries;
            return View(stageSummaries);
        }

        // 6. Sales / Conversion Report
        public async Task<IActionResult> ConversionReport(bool export = false)
        {
            var leadQuery = _userScopeService.ApplyLeadScope(_context.Leads.AsQueryable());
            var oppQuery = _userScopeService.ApplyOpportunityScope(_context.Opportunities.AsQueryable());

            var leads = await leadQuery.ToListAsync();
            var opps = await oppQuery.ToListAsync();

            var model = new ConversionReportViewModel
            {
                TotalLeads = leads.Count,
                ConvertedLeads = leads.Count(l => l.Status == "Converted"),
                LostLeads = leads.Count(l => l.Status == "Lost" || l.Status == "Unqualified"),
                ActiveLeads = leads.Count(l => l.Status != "Converted" && l.Status != "Lost" && l.Status != "Unqualified"),

                TotalOpportunities = opps.Count,
                WonOpportunities = opps.Count(o => o.Status == "Won"),
                LostOpportunities = opps.Count(o => o.Status == "Lost"),
                OpenOpportunities = opps.Count(o => o.Status == "Open"),
                WonRevenue = opps.Where(o => o.Status == "Won").Sum(o => o.Amount)
            };

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    new[] { model },
                    m => new[] { m.TotalLeads.ToString(), m.ConvertedLeads.ToString(), $"{m.LeadConversionRate}%", m.TotalOpportunities.ToString(), m.WonOpportunities.ToString(), $"{m.OpportunityWinRate}%", m.WonRevenue.ToString("F2") },
                    new[] { "TotalLeads", "ConvertedLeads", "LeadConversionRate", "TotalOpps", "WonOpps", "WinRate", "WonRevenue" });
                return File(csv, "text/csv", $"ConversionReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            return View(model);
        }

        // 7. User Activity Report
        public async Task<IActionResult> UserActivityReport(bool export = false)
        {
            var acts = await _context.Activities.Include(a => a.AssignedToUser).ToListAsync();
            var users = await _userManager.Users.ToListAsync();

            var summaries = new List<UserActivitySummary>();
            foreach (var user in users)
            {
                var userActs = acts.Where(a => a.AssignedTo == user.Id).ToList();
                var roles = await _userManager.GetRolesAsync(user);

                summaries.Add(new UserActivitySummary
                {
                    UserName = user.FullName,
                    Role = roles.FirstOrDefault() ?? "SalesExecutive",
                    CallsCount = userActs.Count(a => a.ActivityType == "Call"),
                    MeetingsCount = userActs.Count(a => a.ActivityType == "Meeting"),
                    EmailsCount = userActs.Count(a => a.ActivityType == "Email"),
                    TasksCount = userActs.Count(a => a.ActivityType == "Task")
                });
            }

            summaries = summaries.OrderByDescending(s => s.TotalActivities).ToList();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    summaries,
                    s => new[] { s.UserName, s.Role, s.CallsCount.ToString(), s.MeetingsCount.ToString(), s.EmailsCount.ToString(), s.TasksCount.ToString(), s.TotalActivities.ToString() },
                    new[] { "UserName", "Role", "Calls", "Meetings", "Emails", "Tasks", "TotalActivities" });
                return File(csv, "text/csv", $"UserActivityReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            return View(summaries);
        }

        // 8. Audit Report
        public async Task<IActionResult> AuditReport(string? action, DateTime? startDate, DateTime? endDate, bool export = false)
        {
            if (!_userScopeService.IsAdmin() && !_userScopeService.IsManager())
            {
                return Forbid();
            }

            var query = _context.AuditLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (startDate.HasValue) query = query.Where(a => a.CreatedDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(a => a.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var logs = await query.OrderByDescending(a => a.CreatedDate).Take(500).ToListAsync();

            if (export)
            {
                var csv = _exportService.GenerateCsv(
                    logs,
                    a => new[] { a.AuditLogId.ToString(), a.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"), a.UserName ?? "System", a.Action, a.EntityName, a.RecordId ?? "", a.Result, a.Details ?? "" },
                    new[] { "LogId", "Timestamp", "User", "Action", "Module", "RecordId", "Result", "Details" });
                return File(csv, "text/csv", $"AuditReport_{DateTime.UtcNow:yyyyMMdd}.csv");
            }

            ViewBag.Action = action;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(logs);
        }
    }
}
