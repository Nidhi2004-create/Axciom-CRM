using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [ForeignKey(nameof(LeadId))]
        public virtual Lead? Lead { get; set; }

        [Display(Name = "Related Opportunity")]
        public int? OpportunityId { get; set; }

        [ForeignKey(nameof(OpportunityId))]
        public virtual Opportunity? Opportunity { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Follow-up Date")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [StringLength(50)]
        [Display(Name = "Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Assigned User")]
        public string? AssignedTo { get; set; }

        [ForeignKey(nameof(AssignedTo))]
        public virtual ApplicationUser? AssignedToUser { get; set; }
    }
}
