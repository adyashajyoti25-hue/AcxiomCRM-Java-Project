using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.DTOs;

#region Customer DTOs
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public CustomerStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
    public int ActiveOpportunitiesCount { get; set; }
    public decimal TotalWonValue { get; set; }
}

public class CustomerCreateUpdateDto
{
    public int CustomerId { get; set; }

    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [MaxLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{7,15}$", ErrorMessage = "Enter a valid phone number (7 to 15 digits).")]
    [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [MaxLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [MaxLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}
#endregion

#region Lead DTOs
public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public LeadPriority Priority { get; set; }
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
    public string? Notes { get; set; }
}

public class LeadCreateUpdateDto
{
    public int LeadId { get; set; }

    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [MaxLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{7,15}$", ErrorMessage = "Enter a valid phone number (7 to 15 digits).")]
    [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 100000000, ErrorMessage = "Expected Value must be a positive number.")]
    [Display(Name = "Expected Value")]
    public decimal ExpectedValue { get; set; } = 0;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}

public class LeadConvertDto
{
    [Required]
    public int LeadId { get; set; }

    public bool CreateOpportunity { get; set; } = true;

    [MaxLength(150)]
    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal? OpportunityAmount { get; set; }

    [Display(Name = "Expected Close Date")]
    public DateTime? ExpectedCloseDate { get; set; }

    [Display(Name = "Assign To")]
    public string? AssignedTo { get; set; }

    public string? Remarks { get; set; }
}
#endregion

#region Opportunity DTOs
public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; }
    public int Probability { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
    public decimal WeightedValue => Math.Round(Amount * Probability / 100m, 2);
}

public class OpportunityCreateUpdateDto
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [MaxLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer selection is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}
#endregion

#region FollowUp DTOs
public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public FollowUpType FollowUpType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public FollowUpStatus Status { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool IsOverdue => Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.Today;
    public bool IsUpcoming => Status == FollowUpStatus.Planned && FollowUpDate.Date >= DateTime.Today;
}

public class FollowUpCreateUpdateDto
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [Display(Name = "Follow-up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Follow-up Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Required(ErrorMessage = "Assigned user is required.")]
    [Display(Name = "Assigned User")]
    public string AssignedTo { get; set; } = string.Empty;
}
#endregion

#region Activity DTOs
public class ActivityDto
{
    public int ActivityId { get; set; }
    public ActivityType ActivityType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public ActivityStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ActivityCreateUpdateDto
{
    public int ActivityId { get; set; }

    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [Display(Name = "Activity Date & Time")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Assigned User is required.")]
    [Display(Name = "Assigned User")]
    public string AssignedTo { get; set; } = string.Empty;

    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;
}
#endregion

#region User & Role DTOs
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

public class UserCreateDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UserEditDto
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool UnlockAccount { get; set; }
}

public class ChangePasswordDto
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required(ErrorMessage = "Email or Username is required.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }
}

public class RegisterDto
{
    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
#endregion

#region Audit Log DTOs
public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? IpAddress { get; set; }
}

public class AuditLogFilterDto
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
#endregion

#region Dashboard & Reporting DTOs
public class DashboardMetricsDto
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int UpcomingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    // Chart Data
    public ChartDataDto LeadStatusChart { get; set; } = new();
    public ChartDataDto OpportunityStageChart { get; set; } = new();
    public ChartDataDto MonthlySalesChart { get; set; } = new();

    // Quick lists
    public List<FollowUpDto> RecentFollowUps { get; set; } = new();
    public List<OpportunityDto> TopOpportunities { get; set; } = new();
}

public class ChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}

public class ReportFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? Stage { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
#endregion
