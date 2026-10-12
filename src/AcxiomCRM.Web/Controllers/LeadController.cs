using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public LeadController(ILeadService leadService, IUserService userService)
    {
        _leadService = leadService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, string? assignedTo, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.AssignedTo = assignedTo;

        var users = await _userService.GetSalesExecutivesAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);

        var result = await _leadService.GetLeadsAsync(search, status, assignedTo, page, 10);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new LeadCreateUpdateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(dto);
        }

        var result = await _leadService.CreateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateUsersDropdownAsync();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _leadService.GetForEditAsync(id);
        if (dto == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(dto.AssignedTo);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeadCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        var result = await _leadService.UpdateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = dto.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["ErrorMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dto = new LeadConvertDto
        {
            LeadId = lead.LeadId,
            OpportunityName = $"{lead.CompanyName ?? lead.LeadName} - Opportunity",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = lead.AssignedTo
        };

        ViewBag.Lead = lead;
        await PopulateUsersDropdownAsync(lead.AssignedTo);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertDto dto)
    {
        if (!ModelState.IsValid)
        {
            var lead = await _leadService.GetByIdAsync(dto.LeadId);
            ViewBag.Lead = lead;
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        var result = await _leadService.ConvertLeadAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var lead = await _leadService.GetByIdAsync(dto.LeadId);
            ViewBag.Lead = lead;
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Index", "Customer");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _leadService.DeleteAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdownAsync(string? selectedId = null)
    {
        var users = await _userService.GetSalesExecutivesAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
