using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public ActivityController(
        IActivityService activityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService)
    {
        _activityService = activityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? type, string? status, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Status = status;

        var result = await _activityService.GetActivitiesAsync(search, type, status, page, 10);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var act = await _activityService.GetByIdAsync(id);
        if (act == null)
        {
            TempData["ErrorMessage"] = "Activity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }
        return View(act);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId);
        var dto = new ActivityCreateUpdateDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _activityService.CreateAsync(dto);
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
        var act = await _activityService.GetByIdAsync(id);
        if (act == null)
        {
            TempData["ErrorMessage"] = "Activity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(act.CustomerId, act.LeadId, act.AssignedTo);

        var dto = new ActivityCreateUpdateDto
        {
            ActivityId = act.ActivityId,
            ActivityType = act.ActivityType,
            Subject = act.Subject,
            Description = act.Description,
            ActivityDate = act.ActivityDate,
            CustomerId = act.CustomerId,
            LeadId = act.LeadId,
            AssignedTo = act.AssignedTo,
            Status = act.Status
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _activityService.UpdateAsync(dto);
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
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _activityService.DeleteAsync(id);
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
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
