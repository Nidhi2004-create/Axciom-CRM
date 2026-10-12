using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(150)]
        [Display(Name = "User")]
        public string? UserName { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

        [Required]
        [StringLength(100)]
        [Display(Name = "Module / Entity")]
        public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User, Auth

        [StringLength(100)]
        [Display(Name = "Record ID")]
        public string? RecordId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "Old Value")]
        public string? OldValue { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "New Value")]
        public string? NewValue { get; set; }

        [Display(Name = "Timestamp")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [StringLength(50)]
        public string Result { get; set; } = "Success"; // Success, Failure, LockedOut

        [StringLength(1000)]
        public string? Details { get; set; }
    }
}
