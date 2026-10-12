using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? AssignedTo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation
    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? AssignedTo { get; set; }

    public int? ConvertedCustomerId { get; set; }

    public int? ConvertedOpportunityId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Navigation
    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [ForeignKey(nameof(ConvertedCustomerId))]
    public virtual Customer? ConvertedCustomer { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100)]
    public int Probability { get; set; } = 10;

    public DateTime ExpectedCloseDate { get; set; }

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? AssignedTo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [NotMapped]
    public decimal WeightedValue => Math.Round(Amount * Probability / 100m, 2);

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    public int? OpportunityId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Required]
    [MaxLength(450)]
    public string AssignedTo { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [ForeignKey(nameof(OpportunityId))]
    public virtual Opportunity? Opportunity { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }
}

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    [MaxLength(450)]
    public string AssignedTo { get; set; } = string.Empty;

    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }
}

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(150)]
    public string? UserEmail { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
