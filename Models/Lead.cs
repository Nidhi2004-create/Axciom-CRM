using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [StringLength(50)]
        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

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

        [StringLength(100)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign, Partner, Other

        [Required(ErrorMessage = "Lead Status is required.")]
        [StringLength(50)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [StringLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Range(0, 9999999999.99, ErrorMessage = "Expected Value must be a positive number.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Expected Value")]
        public decimal ExpectedValue { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [ForeignKey(nameof(AssignedTo))]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        // Optional conversion link to Customer
        public int? ConvertedCustomerId { get; set; }
        [ForeignKey(nameof(ConvertedCustomerId))]
        public virtual Customer? ConvertedCustomer { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
