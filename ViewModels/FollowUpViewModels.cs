using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpIndexViewModel
    {
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? AssignedUserFilter { get; set; }
        public DateTime? DateFilter { get; set; }

        public int OverdueCount { get; set; }
        public int UpcomingCount { get; set; }
        public int CompletedCount { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class FollowUpCreateEditViewModel
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity (Optional)")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [Display(Name = "Communication Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [StringLength(1000)]
        [Display(Name = "Remarks / Agenda")]
        public string? Remarks { get; set; }

        [Required]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }
    }

    public class RescheduleFollowUpViewModel
    {
        [Required]
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "New follow-up date is required.")]
        [Display(Name = "New Scheduled Date & Time")]
        public DateTime NewFollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Display(Name = "Reason for Rescheduling")]
        public string? RescheduleReason { get; set; }
    }
}
