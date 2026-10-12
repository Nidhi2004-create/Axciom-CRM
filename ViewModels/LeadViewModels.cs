using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class LeadIndexViewModel
    {
        public List<Lead> Leads { get; set; } = new List<Lead>();
        public string? SearchTerm { get; set; }
        public string? CompanyFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? AssignedUserFilter { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class LeadCreateEditViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Code")]
        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number (starting with 6-9).")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required]
        [Display(Name = "Lead Source")]
        public string Source { get; set; } = "Website";

        [Required]
        [Display(Name = "Lead Status")]
        public string Status { get; set; } = "New";

        [Required]
        public string Priority { get; set; } = "Medium";

        [Range(0, 9999999999.99, ErrorMessage = "Expected Value must be 0 or a positive number.")]
        [Display(Name = "Expected Value (₹)")]
        public decimal ExpectedValue { get; set; } = 0;

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ConvertLeadViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        [Display(Name = "Create an Opportunity upon conversion?")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Range(0.01, 9999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Opportunity Amount (₹)")]
        public decimal? OpportunityAmount { get; set; }

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int OpportunityProbability { get; set; } = 25;

        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Owner / Assigned To")]
        public string? AssignedTo { get; set; }
    }
}
