using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityIndexViewModel
    {
        public List<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public string? SearchTerm { get; set; }
        public int? CustomerFilter { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedUserFilter { get; set; }

        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class OpportunityCreateEditViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Linked Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Pipeline Stage is required.")]
        [Display(Name = "Pipeline Stage")]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 25;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        [Display(Name = "Deal Status")]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        [Display(Name = "Deal Owner / Assigned To")]
        public string? AssignedTo { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notes & Deal Terms")]
        public string? Notes { get; set; }
    }
}
