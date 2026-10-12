using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clean database design
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            b.Property(u => u.Department).HasMaxLength(100);
        });
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        // Customer Configuration
        builder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.HasIndex(c => c.CustomerCode).IsUnique();
            b.HasIndex(c => c.Email).IsUnique();
            b.HasIndex(c => c.Phone).IsUnique();
            b.HasIndex(c => c.Status);
            b.HasIndex(c => c.AssignedTo);

            b.HasOne(c => c.AssignedUser)
                .WithMany(u => u.AssignedCustomers)
                .HasForeignKey(c => c.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Lead Configuration
        builder.Entity<Lead>(b =>
        {
            b.ToTable("Leads");
            b.HasIndex(l => l.LeadCode).IsUnique();
            b.HasIndex(l => l.Email);
            b.HasIndex(l => l.Phone);
            b.HasIndex(l => l.Status);
            b.HasIndex(l => l.AssignedTo);

            b.HasOne(l => l.AssignedUser)
                .WithMany(u => u.AssignedLeads)
                .HasForeignKey(l => l.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(l => l.ConvertedCustomer)
                .WithMany()
                .HasForeignKey(l => l.ConvertedCustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Opportunity Configuration
        builder.Entity<Opportunity>(b =>
        {
            b.ToTable("Opportunities");
            b.HasIndex(o => o.CustomerId);
            b.HasIndex(o => o.Stage);
            b.HasIndex(o => o.Status);
            b.HasIndex(o => o.AssignedTo);

            b.HasOne(o => o.Customer)
                .WithMany(c => c.Opportunities)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(o => o.Lead)
                .WithMany()
                .HasForeignKey(o => o.LeadId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(o => o.AssignedUser)
                .WithMany(u => u.AssignedOpportunities)
                .HasForeignKey(o => o.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // FollowUp Configuration
        builder.Entity<FollowUp>(b =>
        {
            b.ToTable("FollowUps");
            b.HasIndex(f => f.CustomerId);
            b.HasIndex(f => f.LeadId);
            b.HasIndex(f => f.OpportunityId);
            b.HasIndex(f => f.FollowUpDate);
            b.HasIndex(f => f.Status);
            b.HasIndex(f => f.AssignedTo);

            b.HasOne(f => f.Customer)
                .WithMany(c => c.FollowUps)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(f => f.Lead)
                .WithMany(l => l.FollowUps)
                .HasForeignKey(f => f.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(f => f.Opportunity)
                .WithMany(o => o.FollowUps)
                .HasForeignKey(f => f.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(f => f.AssignedUser)
                .WithMany(u => u.AssignedFollowUps)
                .HasForeignKey(f => f.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Activity Configuration
        builder.Entity<Activity>(b =>
        {
            b.ToTable("Activities");
            b.HasIndex(a => a.CustomerId);
            b.HasIndex(a => a.LeadId);
            b.HasIndex(a => a.ActivityDate);
            b.HasIndex(a => a.AssignedTo);

            b.HasOne(a => a.Customer)
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Lead)
                .WithMany(l => l.Activities)
                .HasForeignKey(a => a.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.AssignedUser)
                .WithMany(u => u.AssignedActivities)
                .HasForeignKey(a => a.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog Configuration
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasIndex(al => al.UserId);
            b.HasIndex(al => al.Action);
            b.HasIndex(al => al.EntityName);
            b.HasIndex(al => al.CreatedDate);

            b.HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
