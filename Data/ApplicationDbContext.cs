using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Lead> Leads { get; set; } = null!;
        public DbSet<Opportunity> Opportunities { get; set; } = null!;
        public DbSet<FollowUp> FollowUps { get; set; } = null!;
        public DbSet<Activity> Activities { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer indexes and relationships
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();
                entity.HasIndex(c => c.CustomerCode);

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Lead relationships
            builder.Entity<Lead>(entity =>
            {
                entity.HasIndex(l => l.Email);
                entity.HasIndex(l => l.Phone);
                entity.HasIndex(l => l.LeadCode);
                entity.HasIndex(l => l.Status);

                entity.HasOne(l => l.AssignedToUser)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(l => l.ConvertedCustomer)
                    .WithMany(c => c.Leads)
                    .HasForeignKey(l => l.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Opportunity relationships
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasIndex(o => o.Stage);
                entity.HasIndex(o => o.Status);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany(l => l.Opportunities)
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.AssignedToUser)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FollowUp relationships
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasIndex(f => f.FollowUpDate);
                entity.HasIndex(f => f.Status);

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(f => f.OpportunityId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.AssignedToUser)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Activity relationships
            builder.Entity<Activity>(entity =>
            {
                entity.HasIndex(a => a.ActivityDate);
                entity.HasIndex(a => a.ActivityType);

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AssignedToUser)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AuditLog indexes
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Action);
            });
        }
    }
}
