using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class OpportunityController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public OpportunityController(
        IOpportunityService opportunityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService)
    {
        _opportunityService = opportunityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? stage, string? status, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;

        var result = await _opportunityService.GetOpportunitiesAsync(search, stage, status, page, 10);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }
        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync(customerId);
        var dto = new OpportunityCreateUpdateDto
        {
            CustomerId = customerId ?? 0,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _opportunityService.CreateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _opportunityService.GetForEditAsync(id);
        if (dto == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OpportunityCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        var result = await _opportunityService.UpdateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdownsAsync(dto.CustomerId, dto.LeadId, dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = dto.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _opportunityService.DeleteAsync(id);
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
