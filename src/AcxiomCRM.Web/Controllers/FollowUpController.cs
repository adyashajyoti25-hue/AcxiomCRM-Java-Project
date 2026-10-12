using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class FollowUpController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public FollowUpController(
        IFollowUpService followUpService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService)
    {
        _followUpService = followUpService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string? type, DateTime? date, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Type = type;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");

        var result = await _followUpService.GetFollowUpsAsync(search, status, type, date, page, 10);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId);
        var dto = new FollowUpCreateUpdateDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.Today.AddDays(1)
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _followUpService.CreateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _followUpService.GetForEditAsync(id);
        if (dto == null)
        {
            TempData["ErrorMessage"] = "Follow-up not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FollowUpCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _followUpService.UpdateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var result = await _followUpService.CompleteAsync(id, remarks);
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remarks)
    {
        var result = await _followUpService.CancelAsync(id, remarks);
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkMissed(int id)
    {
        var result = await _followUpService.MarkMissedAsync(id);
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Upcoming()
    {
        var list = await _followUpService.GetUpcomingAsync(20);
        return View("UpcomingOverdueList", (list, "Upcoming Follow-Ups"));
    }

    [HttpGet]
    public async Task<IActionResult> Overdue()
    {
        var list = await _followUpService.GetOverdueAsync(20);
        return View("UpcomingOverdueList", (list, "Overdue Follow-Ups"));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, string? assignedTo = null)
    {
        var customers = await _customerService.GetAllLookupAsync();
        ViewBag.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _leadService.GetAllLookupAsync();
        ViewBag.LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId);

        var users = await _userService.GetSalesExecutivesAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
    }
}
