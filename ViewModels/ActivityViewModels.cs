using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class ActivityIndexViewModel
    {
        public List<Activity> Activities { get; set; } = new List<Activity>();
        public string? SearchTerm { get; set; }
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public DateTime? DateFilter { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class ActivityCreateEditViewModel
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Detailed Notes")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Date & Time")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }

        [Required]
        public string Status { get; set; } = "Completed"; // Pending, Completed, Cancelled
    }
}
