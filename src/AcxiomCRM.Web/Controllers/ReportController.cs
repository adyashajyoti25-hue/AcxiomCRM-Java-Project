using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public ReportController(IReportService reportService, IAuditService auditService, ICurrentUserService currentUser)
    {
        _reportService = reportService;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CustomerReport(ReportFilterDto filter)
    {
        ViewBag.Filter = filter;
        var data = await _reportService.GetCustomerReportAsync(filter);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCustomerReport(ReportFilterDto filter)
    {
        filter.PageSize = 10000;
        var data = await _reportService.GetCustomerReportAsync(filter);
        var bytes = await _reportService.ExportToCsvAsync(data.Items, "AcxiomCRM Customer Report");
        return File(bytes, "text/csv", $"CustomerReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> LeadReport(ReportFilterDto filter)
    {
        ViewBag.Filter = filter;
        var data = await _reportService.GetLeadReportAsync(filter);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeadReport(ReportFilterDto filter)
    {
        filter.PageSize = 10000;
        var data = await _reportService.GetLeadReportAsync(filter);
        var bytes = await _reportService.ExportToCsvAsync(data.Items, "AcxiomCRM Lead Report");
        return File(bytes, "text/csv", $"LeadReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> FollowUpReport(ReportFilterDto filter)
    {
        ViewBag.Filter = filter;
        var data = await _reportService.GetFollowUpReportAsync(filter);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportFollowUpReport(ReportFilterDto filter)
    {
        filter.PageSize = 10000;
        var data = await _reportService.GetFollowUpReportAsync(filter);
        var bytes = await _reportService.ExportToCsvAsync(data.Items, "AcxiomCRM Follow-Up Report");
        return File(bytes, "text/csv", $"FollowUpReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> OpportunityReport(ReportFilterDto filter)
    {
        ViewBag.Filter = filter;
        var data = await _reportService.GetOpportunityReportAsync(filter);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> ExportOpportunityReport(ReportFilterDto filter)
    {
        filter.PageSize = 10000;
        var data = await _reportService.GetOpportunityReportAsync(filter);
        var bytes = await _reportService.ExportToCsvAsync(data.Items, "AcxiomCRM Opportunity Report");
        return File(bytes, "text/csv", $"OpportunityReport_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> PipelineReport()
    {
        var data = await _reportService.GetPipelineReportAsync();
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> ConversionReport()
    {
        var data = await _reportService.GetConversionReportAsync();
        return View(data);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> UserActivityReport()
    {
        var data = await _reportService.GetUserActivityReportAsync();
        return View(data);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AuditReport(AuditLogFilterDto filter)
    {
        ViewBag.Filter = filter;
        var data = await _auditService.GetLogsAsync(filter);
        return View(data);
    }
}
