using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }

    // Navigation properties
    public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
    public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
