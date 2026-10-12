using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class ReportsHubViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public decimal ConversionRate => TotalLeads > 0 ? Math.Round(((decimal)ConvertedLeads / TotalLeads) * 100, 1) : 0;
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }
        public int CompletedFollowUps { get; set; }
        public int TotalActivities { get; set; }
    }

    public class StagePipelineSummary
    {
        public string Stage { get; set; } = string.Empty;
        public int DealCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class OwnerPipelineSummary
    {
        public string OwnerName { get; set; } = string.Empty;
        public int DealCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public decimal LeadConversionRate => TotalLeads > 0 ? Math.Round(((decimal)ConvertedLeads / TotalLeads) * 100, 1) : 0;

        public int TotalOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public decimal OpportunityWinRate => (WonOpportunities + LostOpportunities) > 0 ? Math.Round(((decimal)WonOpportunities / (WonOpportunities + LostOpportunities)) * 100, 1) : 0;
        public decimal WonRevenue { get; set; }
    }

    public class UserActivitySummary
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int TotalActivities => CallsCount + MeetingsCount + EmailsCount + TasksCount;
    }
}
