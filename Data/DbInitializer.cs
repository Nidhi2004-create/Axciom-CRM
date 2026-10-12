using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Automatically apply pending migrations
                await context.Database.MigrateAsync();

                // 1. Seed Roles
                string[] roleNames = { ApplicationRole.Admin, ApplicationRole.Manager, ApplicationRole.SalesExecutive };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new ApplicationRole(roleName));
                    }
                }

                // 2. Seed Default Users
                var adminUser = await userManager.FindByEmailAsync("admin@acxiom.com");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin@acxiom.com",
                        Email = "admin@acxiom.com",
                        FullName = "System Administrator",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, ApplicationRole.Admin);
                    }
                }

                var managerUser = await userManager.FindByEmailAsync("manager@acxiom.com");
                if (managerUser == null)
                {
                    managerUser = new ApplicationUser
                    {
                        UserName = "manager@acxiom.com",
                        Email = "manager@acxiom.com",
                        FullName = "Sales Manager",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(managerUser, "Manager@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(managerUser, ApplicationRole.Manager);
                    }
                }

                var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
                if (salesUser == null)
                {
                    salesUser = new ApplicationUser
                    {
                        UserName = "sales@acxiom.com",
                        Email = "sales@acxiom.com",
                        FullName = "Alex Morgan",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(salesUser, "Sales@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(salesUser, ApplicationRole.SalesExecutive);
                    }
                }

                // 3. Seed Initial Demo Data if empty
                if (!await context.Customers.AnyAsync())
                {
                    var customer1 = new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Apex Global Enterprises",
                        Email = "contact@apexglobal.com",
                        Phone = "9876543210",
                        CompanyName = "Apex Global Corp",
                        Address = "104 Tech Park, Sector 5",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = salesUser?.Id
                    };

                    var customer2 = new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Zenith Dynamics Ltd",
                        Email = "info@zenithdynamics.com",
                        Phone = "9812345678",
                        CompanyName = "Zenith Dynamics",
                        Address = "45 Ring Road",
                        City = "Pune",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedBy = salesUser?.Id
                    };

                    var customer3 = new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Nexus Cloud Solutions",
                        Email = "hello@nexuscloud.io",
                        Phone = "9765432109",
                        CompanyName = "Nexus Cloud",
                        Address = "72 Cyber City, DLF Phase 2",
                        City = "Gurugram",
                        State = "Haryana",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        CreatedBy = managerUser?.Id
                    };

                    context.Customers.AddRange(customer1, customer2, customer3);
                    await context.SaveChangesAsync();

                    // Seed Leads
                    var lead1 = new Lead
                    {
                        LeadCode = "LEAD-2001",
                        LeadName = "Sophia Reynolds",
                        Email = "sophia@innovatech.com",
                        Phone = "9823456781",
                        CompanyName = "InnovaTech Labs",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 450000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        AssignedTo = salesUser?.Id,
                        Notes = "Interested in enterprise CRM migration."
                    };

                    var lead2 = new Lead
                    {
                        LeadCode = "LEAD-2002",
                        LeadName = "Marcus Brody",
                        Email = "marcus@bluehorizons.org",
                        Phone = "9834567892",
                        CompanyName = "Blue Horizon Logistics",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 280000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-7),
                        AssignedTo = salesUser?.Id,
                        Notes = "Requested demo presentation next week."
                    };

                    var lead3 = new Lead
                    {
                        LeadCode = "LEAD-2003",
                        LeadName = "Kavita Rao",
                        Email = "kavita@summitretail.in",
                        Phone = "9845678903",
                        CompanyName = "Summit Retail",
                        Source = "Campaign",
                        Status = "New",
                        Priority = "Low",
                        ExpectedValue = 150000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        AssignedTo = managerUser?.Id,
                        Notes = "Inbound query from marketing newsletter."
                    };

                    context.Leads.AddRange(lead1, lead2, lead3);
                    await context.SaveChangesAsync();

                    // Seed Opportunities
                    var opp1 = new Opportunity
                    {
                        OpportunityName = "Apex Cloud Migration Deal",
                        CustomerId = customer1.CustomerId,
                        Amount = 650000m,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-18),
                        AssignedTo = salesUser?.Id,
                        Notes = "Final SLA review in progress."
                    };

                    var opp2 = new Opportunity
                    {
                        OpportunityName = "Zenith ERP Integration",
                        CustomerId = customer2.CustomerId,
                        Amount = 420000m,
                        Stage = "Proposal",
                        Probability = 50,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedTo = salesUser?.Id,
                        Notes = "Custom API connectors requirement."
                    };

                    var opp3 = new Opportunity
                    {
                        OpportunityName = "Nexus Annual Enterprise License",
                        CustomerId = customer3.CustomerId,
                        Amount = 890000m,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-3),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddDays(-28),
                        AssignedTo = managerUser?.Id,
                        Notes = "Contract signed and initial payment received."
                    };

                    context.Opportunities.AddRange(opp1, opp2, opp3);
                    await context.SaveChangesAsync();

                    // Seed Follow-ups
                    var followUp1 = new FollowUp
                    {
                        CustomerId = customer1.CustomerId,
                        OpportunityId = opp1.OpportunityId,
                        Subject = "Contract review discussion",
                        FollowUpDate = DateTime.Today.AddDays(2).AddHours(11),
                        FollowUpType = "Meeting",
                        Status = "Planned",
                        Remarks = "Discuss warranty and payment terms.",
                        AssignedTo = salesUser?.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-3)
                    };

                    var followUp2 = new FollowUp
                    {
                        LeadId = lead1.LeadId,
                        Subject = "Follow-up on product quotation",
                        FollowUpDate = DateTime.Today.AddDays(1).AddHours(15),
                        FollowUpType = "Call",
                        Status = "Planned",
                        Remarks = "Confirm timeline for trial rollout.",
                        AssignedTo = salesUser?.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    };

                    var followUp3 = new FollowUp
                    {
                        CustomerId = customer2.CustomerId,
                        Subject = "Technical specification call",
                        FollowUpDate = DateTime.Today.AddDays(-1).AddHours(14),
                        FollowUpType = "Call",
                        Status = "Completed",
                        CompletedDate = DateTime.Today.AddDays(-1).AddHours(15),
                        Remarks = "All technical requirements answered.",
                        AssignedTo = salesUser?.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    };

                    context.FollowUps.AddRange(followUp1, followUp2, followUp3);

                    // Seed Activities
                    var act1 = new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "Executive Intro with Apex Leadership",
                        Description = "Presented CRM solution capabilities and integration roadmap.",
                        ActivityDate = DateTime.UtcNow.AddDays(-15),
                        CustomerId = customer1.CustomerId,
                        AssignedTo = salesUser?.Id,
                        Status = "Completed"
                    };

                    var act2 = new Activity
                    {
                        ActivityType = "Email",
                        Subject = "Proposal document sent to Zenith Dynamics",
                        Description = "Sent formal commercial proposal document v1.2.",
                        ActivityDate = DateTime.UtcNow.AddDays(-8),
                        CustomerId = customer2.CustomerId,
                        AssignedTo = salesUser?.Id,
                        Status = "Completed"
                    };

                    context.Activities.AddRange(act1, act2);

                    // Seed initial audit log
                    context.AuditLogs.Add(new AuditLog
                    {
                        UserId = adminUser?.Id,
                        UserName = adminUser?.Email,
                        Action = "Security",
                        EntityName = "System",
                        RecordId = "0",
                        CreatedDate = DateTime.UtcNow,
                        Result = "Success",
                        Details = "System initialized and database seeded successfully."
                    });

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
