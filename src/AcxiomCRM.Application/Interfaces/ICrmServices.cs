using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    string? IpAddress { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesExecutive { get; }
}

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? recordId, string? oldValue, string? newValue);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter);
}

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, string? status, int pageIndex, int pageSize);
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<CustomerCreateUpdateDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int Id)> CreateAsync(CustomerCreateUpdateDto dto);
    Task<(bool Success, string Message)> UpdateAsync(CustomerCreateUpdateDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<bool> PhoneExistsAsync(string phone, int? excludeId = null);
    Task<List<CustomerDto>> GetAllLookupAsync();
}

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetLeadsAsync(string? search, string? status, string? assignedTo, int pageIndex, int pageSize);
    Task<LeadDto?> GetByIdAsync(int id);
    Task<LeadCreateUpdateDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int Id)> CreateAsync(LeadCreateUpdateDto dto);
    Task<(bool Success, string Message)> UpdateAsync(LeadCreateUpdateDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<(bool Success, string Message)> ConvertLeadAsync(LeadConvertDto dto);
    Task<List<LeadDto>> GetAllLookupAsync();
}

public interface IOpportunityService
{
    Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(string? search, string? stage, string? status, int pageIndex, int pageSize);
    Task<OpportunityDto?> GetByIdAsync(int id);
    Task<OpportunityCreateUpdateDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int Id)> CreateAsync(OpportunityCreateUpdateDto dto);
    Task<(bool Success, string Message)> UpdateAsync(OpportunityCreateUpdateDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

public interface IFollowUpService
{
    Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(string? search, string? status, string? type, DateTime? date, int pageIndex, int pageSize);
    Task<FollowUpDto?> GetByIdAsync(int id);
    Task<FollowUpCreateUpdateDto?> GetForEditAsync(int id);
    Task<(bool Success, string Message, int Id)> CreateAsync(FollowUpCreateUpdateDto dto);
    Task<(bool Success, string Message)> UpdateAsync(FollowUpCreateUpdateDto dto);
    Task<(bool Success, string Message)> CompleteAsync(int id, string? remarks);
    Task<(bool Success, string Message)> CancelAsync(int id, string? remarks);
    Task<(bool Success, string Message)> MarkMissedAsync(int id);
    Task<List<FollowUpDto>> GetUpcomingAsync(int count = 5);
    Task<List<FollowUpDto>> GetOverdueAsync(int count = 5);
}

public interface IActivityService
{
    Task<PagedResult<ActivityDto>> GetActivitiesAsync(string? search, string? type, string? status, int pageIndex, int pageSize);
    Task<ActivityDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, int Id)> CreateAsync(ActivityCreateUpdateDto dto);
    Task<(bool Success, string Message)> UpdateAsync(ActivityCreateUpdateDto dto);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(string? search);
    Task<UserDto?> GetByIdAsync(string id);
    Task<(bool Success, string Message, string Id)> CreateUserAsync(UserCreateDto dto);
    Task<(bool Success, string Message)> UpdateUserAsync(UserEditDto dto);
    Task<(bool Success, string Message)> ToggleStatusAsync(string id);
    Task<(bool Success, string Message)> ResetPasswordAsync(ChangePasswordDto dto);
    Task<List<ApplicationUser>> GetSalesExecutivesAsync();
}

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(string? dateFilter = null);
}

public interface IReportService
{
    Task<PagedResult<CustomerDto>> GetCustomerReportAsync(ReportFilterDto filter);
    Task<PagedResult<LeadDto>> GetLeadReportAsync(ReportFilterDto filter);
    Task<PagedResult<FollowUpDto>> GetFollowUpReportAsync(ReportFilterDto filter);
    Task<PagedResult<OpportunityDto>> GetOpportunityReportAsync(ReportFilterDto filter);
    Task<object> GetPipelineReportAsync();
    Task<object> GetConversionReportAsync();
    Task<object> GetUserActivityReportAsync();
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string reportTitle);
}
