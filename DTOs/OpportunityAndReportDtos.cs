using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedAmount { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? OwnerName { get; set; }
    }

    public class OpportunityCreateDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer ID is required.")]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Pipeline Stage is required.")]
        public string Stage { get; set; } = "Qualification";

        [Required]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string FollowUpType { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public string? AssignedUserName { get; set; }
        public string? Remarks { get; set; }
    }

    public class FollowUpCreateDto
    {
        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Follow-up Type is required.")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Follow-up Date is required.")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public int? OpportunityId { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }

    public class PipelineReportApiDto
    {
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedPipelineValue { get; set; }
        public int TotalDealsCount { get; set; }
        public List<StageSummaryDto> StageBreakdown { get; set; } = new List<StageSummaryDto>();
    }

    public class StageSummaryDto
    {
        public string Stage { get; set; } = string.Empty;
        public int DealCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }
}
