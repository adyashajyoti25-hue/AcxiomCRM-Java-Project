using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public AuditController(IAuditService auditService, IUserService userService)
    {
        _auditService = auditService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditLogFilterDto filter)
    {
        ViewBag.Filter = filter;
        var users = await _userService.GetUsersAsync(null);
        ViewBag.Users = users;

        var result = await _auditService.GetLogsAsync(filter);
        return View(result);
    }
}
