using System.Security.Claims;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;

namespace AcxiomCRM.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    public bool IsAdmin => User?.IsInRole(AppRoles.Admin) ?? false;

    public bool IsManager => User?.IsInRole(AppRoles.Manager) ?? false;

    public bool IsSalesExecutive => User?.IsInRole(AppRoles.SalesExecutive) ?? false;
}
