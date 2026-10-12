using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // KPI Metrics
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }
        public int PendingFollowUpsCount { get; set; }
        public int OverdueFollowUpsCount { get; set; }

        // Role-Specific Metrics
        public int TotalUsersCount { get; set; }
        public int TotalAuditEventsCount { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;

        // Date Filter options
        public string DateFilter { get; set; } = "ThisMonth"; // Today, ThisWeek, ThisMonth, Custom, All
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }

        // Chart Data (Chart.js)
        public List<string> LeadStatusLabels { get; set; } = new List<string>();
        public List<int> LeadStatusCounts { get; set; } = new List<int>();

        public List<string> PipelineStageLabels { get; set; } = new List<string>();
        public List<decimal> PipelineStageAmounts { get; set; } = new List<decimal>();

        public List<string> MonthlySalesLabels { get; set; } = new List<string>();
        public List<decimal> MonthlySalesAmounts { get; set; } = new List<decimal>();

        // Lists for active workbench
        public List<FollowUp> UpcomingFollowUps { get; set; } = new List<FollowUp>();
        public List<Activity> RecentActivities { get; set; } = new List<Activity>();
    }
}
