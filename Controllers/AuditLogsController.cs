using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;

        public AuditLogsController(ApplicationDbContext context, IExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index(
            string? userFilter,
            string? moduleFilter,
            string? actionFilter,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.UserName != null && a.UserName.Contains(userFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                query = query.Where(a => a.EntityName == moduleFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int pageSize = 20;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.UserFilter = userFilter;
            ViewBag.ModuleFilter = moduleFilter;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(logs);
        }

        // GET: AuditLogs/Export
        public async Task<IActionResult> Export(
            string? userFilter,
            string? moduleFilter,
            string? actionFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userFilter)) query = query.Where(a => a.UserName != null && a.UserName.Contains(userFilter.Trim()));
            if (!string.IsNullOrWhiteSpace(moduleFilter)) query = query.Where(a => a.EntityName == moduleFilter);
            if (!string.IsNullOrWhiteSpace(actionFilter)) query = query.Where(a => a.Action == actionFilter);
            if (startDate.HasValue) query = query.Where(a => a.CreatedDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(a => a.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderByDescending(a => a.CreatedDate).Take(1000).ToListAsync();

            var csvBytes = _exportService.GenerateCsv(
                data,
                a => new[]
                {
                    a.AuditLogId.ToString(),
                    a.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    a.UserName ?? "System",
                    a.Action,
                    a.EntityName,
                    a.RecordId ?? "",
                    a.Result,
                    a.Details ?? "",
                    a.IpAddress ?? ""
                },
                new[] { "LogId", "TimestampUtc", "User", "Action", "Module", "RecordId", "Result", "Details", "IpAddress" });

            return File(csvBytes, "text/csv", $"AcxiomCRM_AuditLog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}
