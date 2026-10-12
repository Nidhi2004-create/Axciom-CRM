using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string? userId,
            string? userName,
            string action,
            string entityName,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string result = "Success",
            string? details = null,
            string? ipAddress = null);

        Task<List<AuditLog>> GetAuditLogsAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string? userId,
            string? userName,
            string action,
            string entityName,
            string? recordId = null,
            string? oldValue = null,
            string? newValue = null,
            string result = "Success",
            string? details = null,
            string? ipAddress = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (string.IsNullOrEmpty(ipAddress) && httpContext != null)
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                }

                if (string.IsNullOrEmpty(userName) && httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    userName = httpContext.User.Identity.Name;
                }

                var log = new AuditLog
                {
                    UserId = userId,
                    UserName = userName ?? "System",
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress ?? "127.0.0.1",
                    Result = result,
                    Details = details
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never let audit log failures crash the main business request
            }
        }

        public Task<List<AuditLog>> GetAuditLogsAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.UserName != null && a.UserName.Contains(userFilter));
            }

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                query = query.Where(a => a.EntityName.Contains(moduleFilter));
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action.Contains(actionFilter));
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate <= endDate.Value.AddDays(1).AddTicks(-1));
            }

            return Task.FromResult(query.OrderByDescending(a => a.CreatedDate).Take(500).ToList());
        }
    }
}
