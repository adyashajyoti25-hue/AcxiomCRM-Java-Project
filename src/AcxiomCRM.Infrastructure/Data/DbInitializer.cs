using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // Ensure database created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = { AppRoles.Admin, AppRoles.Manager, AppRoles.SalesExecutive };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName, $"Default system role: {roleName}");
                await roleManager.CreateAsync(role);
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.com",
                Email = "admin@acxiomcrm.com",
                FullName = "System Administrator",
                Department = "Executive IT",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.com",
                Email = "manager@acxiomcrm.com",
                FullName = "Marcus Vance (Sales Manager)",
                Department = "Sales Management",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, AppRoles.Manager);
            }
        }

        var execUser = await userManager.FindByEmailAsync("executive@acxiomcrm.com");
        if (execUser == null)
        {
            execUser = new ApplicationUser
            {
                UserName = "executive@acxiomcrm.com",
                Email = "executive@acxiomcrm.com",
                FullName = "Alex Executive",
                Department = "Enterprise Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(execUser, "Executive@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(execUser, AppRoles.SalesExecutive);
            }
        }

        var sarahUser = await userManager.FindByEmailAsync("sarah@acxiomcrm.com");
        if (sarahUser == null)
        {
            sarahUser = new ApplicationUser
            {
                UserName = "sarah@acxiomcrm.com",
                Email = "sarah@acxiomcrm.com",
                FullName = "Sarah Johnson",
                Department = "Commercial Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(sarahUser, "Sarah@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sarahUser, AppRoles.SalesExecutive);
            }
        }

        // 3. Seed Customers if none exist
        if (!await context.Customers.AnyAsync())
        {
            var cust1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Apex Global Technologies",
                Email = "contact@apextech.com",
                Phone = "9876543210",
                CompanyName = "Apex Global Solutions Inc.",
                Address = "104 Cyber Gateway, HITEC City",
                City = "Hyderabad",
                State = "Telangana",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                CreatedBy = adminUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Strategic account exploring multi-region cloud rollout."
            };

            var cust2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "BlueHorizon Retailers",
                Email = "procure@bluehorizon.org",
                Phone = "9812345678",
                CompanyName = "BlueHorizon Omnichannel Ltd.",
                Address = "52 MG Road, Indiranagar",
                City = "Bengaluru",
                State = "Karnataka",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                CreatedBy = managerUser.Id,
                AssignedTo = sarahUser.Id,
                Notes = "High volume retail merchant upgrading POS analytics."
            };

            var cust3 = new Customer
            {
                CustomerCode = "CUST-1003",
                CustomerName = "Nexus Pharma Labs",
                Email = "info@nexuspharma.io",
                Phone = "9900112233",
                CompanyName = "Nexus Pharmaceuticals",
                Address = "88 BKC Bandra",
                City = "Mumbai",
                State = "Maharashtra",
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                CreatedBy = adminUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Compliance focused customer requiring audit trail SLA."
            };

            context.Customers.AddRange(cust1, cust2, cust3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var lead1 = new Lead
            {
                LeadCode = "LEAD-2001",
                LeadName = "FinTech Dynamics Pvt Ltd",
                Email = "lead@fintechdyn.com",
                Phone = "9123456780",
                CompanyName = "FinTech Dynamics",
                Source = LeadSource.Website,
                Status = LeadStatus.Qualified,
                Priority = LeadPriority.High,
                ExpectedValue = 450000.00m,
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CreatedBy = execUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Inbound contact requesting enterprise demo."
            };

            var lead2 = new Lead
            {
                LeadCode = "LEAD-2002",
                LeadName = "Starlight Logistics",
                Email = "dispatch@starlightlog.com",
                Phone = "9234567891",
                CompanyName = "Starlight Logistics Corp",
                Source = LeadSource.Referral,
                Status = LeadStatus.Contacted,
                Priority = LeadPriority.Medium,
                ExpectedValue = 250000.00m,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                CreatedBy = sarahUser.Id,
                AssignedTo = sarahUser.Id,
                Notes = "Referred by regional distributor."
            };

            var lead3 = new Lead
            {
                LeadCode = "LEAD-2003",
                LeadName = "Zenith Health Systems",
                Email = "partners@zenithhealth.org",
                Phone = "9345678902",
                CompanyName = "Zenith Healthcare Group",
                Source = LeadSource.Campaign,
                Status = LeadStatus.New,
                Priority = LeadPriority.High,
                ExpectedValue = 600000.00m,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                CreatedBy = execUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Quarterly marketing webinar registrant."
            };

            context.Leads.AddRange(lead1, lead2, lead3);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var opp1 = new Opportunity
            {
                OpportunityName = "Apex Cloud Migration Tier 1",
                CustomerId = cust1.CustomerId,
                Amount = 750000.00m,
                Stage = OpportunityStage.Negotiation,
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-14),
                CreatedBy = execUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Final contract review with enterprise legal counsel."
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "BlueHorizon Omnichannel Suite",
                CustomerId = cust2.CustomerId,
                Amount = 320000.00m,
                Stage = OpportunityStage.Proposal,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = OpportunityStatus.Open,
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CreatedBy = sarahUser.Id,
                AssignedTo = sarahUser.Id,
                Notes = "Custom proposal submitted, waiting for board approval."
            };

            var opp3 = new Opportunity
            {
                OpportunityName = "Nexus Pharma Pilot Expansion",
                CustomerId = cust3.CustomerId,
                Amount = 500000.00m,
                Stage = OpportunityStage.Won,
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-3),
                Status = OpportunityStatus.Won,
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                CreatedBy = execUser.Id,
                AssignedTo = execUser.Id,
                Notes = "Purchase order signed and onboarded."
            };

            context.Opportunities.AddRange(opp1, opp2, opp3);
            await context.SaveChangesAsync();

            // 6. Seed FollowUps
            var fu1 = new FollowUp
            {
                CustomerId = cust1.CustomerId,
                OpportunityId = opp1.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = FollowUpType.Call,
                Subject = "Discuss commercial payment milestones",
                Remarks = "Call CFO to finalize SLA clauses.",
                Status = FollowUpStatus.Planned,
                AssignedTo = execUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            var fu2 = new FollowUp
            {
                LeadId = lead1.LeadId,
                FollowUpDate = DateTime.UtcNow.AddDays(3),
                FollowUpType = FollowUpType.Meeting,
                Subject = "Architecture Review & Security Q&A",
                Remarks = "Bring technical architect into discussion.",
                Status = FollowUpStatus.Planned,
                AssignedTo = execUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            var fu3 = new FollowUp
            {
                CustomerId = cust2.CustomerId,
                FollowUpDate = DateTime.UtcNow.AddDays(-1),
                FollowUpType = FollowUpType.Email,
                Subject = "Send updated feature breakdown sheet",
                Remarks = "Follow-up email pending response.",
                Status = FollowUpStatus.Planned, // overdue planned
                AssignedTo = sarahUser.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-4)
            };

            context.FollowUps.AddRange(fu1, fu2, fu3);

            // 7. Seed Activities
            var act1 = new Activity
            {
                ActivityType = ActivityType.Call,
                Subject = "Introductory Discovery Call",
                Description = "Identified 4 key pain points in existing legacy pipeline.",
                ActivityDate = DateTime.UtcNow.AddDays(-10),
                CustomerId = cust1.CustomerId,
                AssignedTo = execUser.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            };

            var act2 = new Activity
            {
                ActivityType = ActivityType.Meeting,
                Subject = "Solution Architecture Presentation",
                Description = "Presented AcxiomCRM capabilities to CTO and VP.",
                ActivityDate = DateTime.UtcNow.AddDays(-6),
                CustomerId = cust1.CustomerId,
                AssignedTo = execUser.Id,
                Status = ActivityStatus.Completed,
                CreatedDate = DateTime.UtcNow.AddDays(-6)
            };

            context.Activities.AddRange(act1, act2);

            // 8. Seed Initial Audit Logs
            var audit1 = new AuditLog
            {
                UserId = adminUser.Id,
                UserEmail = adminUser.Email,
                Action = AuditActions.Login,
                EntityName = "Authentication",
                RecordId = adminUser.Id,
                NewValue = "Admin logged in successfully",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                IpAddress = "127.0.0.1"
            };

            var audit2 = new AuditLog
            {
                UserId = adminUser.Id,
                UserEmail = adminUser.Email,
                Action = AuditActions.Create,
                EntityName = "Customer",
                RecordId = "CUST-1001",
                NewValue = "Customer 'Apex Global Technologies' created",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                IpAddress = "127.0.0.1"
            };

            var audit3 = new AuditLog
            {
                UserId = execUser.Id,
                UserEmail = execUser.Email,
                Action = AuditActions.Create,
                EntityName = "Opportunity",
                RecordId = "Apex Cloud Migration Tier 1",
                NewValue = "Opportunity created with Amount 750,000",
                CreatedDate = DateTime.UtcNow.AddDays(-14),
                IpAddress = "127.0.0.1"
            };

            context.AuditLogs.AddRange(audit1, audit2, audit3);
            await context.SaveChangesAsync();
        }
    }
}
