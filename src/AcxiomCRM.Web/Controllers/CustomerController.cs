using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;

    public CustomerController(ICustomerService customerService, IUserService userService)
    {
        _customerService = customerService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;

        var result = await _customerService.GetCustomersAsync(search, status, page, 10);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdownAsync();
        return View(new CustomerCreateUpdateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync();
            return View(dto);
        }

        var result = await _customerService.CreateAsync(dto);
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
        var dto = await _customerService.GetForEditAsync(id);
        if (dto == null)
        {
            TempData["ErrorMessage"] = "Customer not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateUsersDropdownAsync(dto.AssignedTo);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        var result = await _customerService.UpdateAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateUsersDropdownAsync(dto.AssignedTo);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = dto.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.DeleteAsync(id);
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
