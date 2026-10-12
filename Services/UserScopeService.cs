using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IUserScopeService
    {
        string? GetCurrentUserId();
        string? GetCurrentUserName();
        bool IsAdmin();
        bool IsManager();
        bool IsSalesExecutive();
        IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query);
        IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query);
        IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query);
        IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query);
        IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query);
    }

    public class UserScopeService : IUserScopeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserScopeService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public string? GetCurrentUserName()
        {
            return User?.Identity?.Name;
        }

        public bool IsAdmin()
        {
            return User?.IsInRole(ApplicationRole.Admin) == true;
        }

        public bool IsManager()
        {
            return User?.IsInRole(ApplicationRole.Manager) == true;
        }

        public bool IsSalesExecutive()
        {
            return User?.IsInRole(ApplicationRole.SalesExecutive) == true;
        }

        public IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query)
        {
            // Admin and Manager can see all; SalesExecutive sees only assigned
            if (IsAdmin() || IsManager())
            {
                return query;
            }

            var userId = GetCurrentUserId();
            return query.Where(c => c.CreatedBy == userId || c.CreatedBy == null);
        }

        public IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query)
        {
            if (IsAdmin() || IsManager())
            {
                return query;
            }

            var userId = GetCurrentUserId();
            return query.Where(l => l.AssignedTo == userId || l.AssignedTo == null);
        }

        public IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query)
        {
            if (IsAdmin() || IsManager())
            {
                return query;
            }

            var userId = GetCurrentUserId();
            return query.Where(o => o.AssignedTo == userId || o.AssignedTo == null);
        }

        public IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query)
        {
            if (IsAdmin() || IsManager())
            {
                return query;
            }

            var userId = GetCurrentUserId();
            return query.Where(f => f.AssignedTo == userId || f.AssignedTo == null);
        }

        public IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query)
        {
            if (IsAdmin() || IsManager())
            {
                return query;
            }

            var userId = GetCurrentUserId();
            return query.Where(a => a.AssignedTo == userId || a.AssignedTo == null);
        }
    }
}
