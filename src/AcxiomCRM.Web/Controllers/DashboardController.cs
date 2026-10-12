using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUser)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? dateFilter = null)
    {
        ViewBag.DateFilter = dateFilter ?? "all";
        ViewBag.UserRole = _currentUser.Role ?? "SalesExecutive";
        ViewBag.UserName = _currentUser.Email;

        var metrics = await _dashboardService.GetDashboardMetricsAsync(dateFilter);
        return View(metrics);
    }
}
