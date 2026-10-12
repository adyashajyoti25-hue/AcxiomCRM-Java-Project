using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var users = await _userService.GetUsersAsync(search);
        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.RolesList = new SelectList(AppRoles.All);
        return View(new UserCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RolesList = new SelectList(AppRoles.All, dto.Role);
            return View(dto);
        }

        var result = await _userService.CreateUserAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.RolesList = new SelectList(AppRoles.All, dto.Role);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.RolesList = new SelectList(AppRoles.All, user.Role);

        var dto = new UserEditDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Department = user.Department,
            Role = user.Role,
            IsActive = user.IsActive
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RolesList = new SelectList(AppRoles.All, dto.Role);
            return View(dto);
        }

        var result = await _userService.UpdateUserAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.RolesList = new SelectList(AppRoles.All, dto.Role);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var result = await _userService.ToggleStatusAsync(id);
        if (result.Success)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserName = user.FullName;
        ViewBag.UserEmail = user.Email;
        return View(new ChangePasswordDto { UserId = user.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var user = await _userService.GetByIdAsync(dto.UserId);
            ViewBag.UserName = user?.FullName;
            ViewBag.UserEmail = user?.Email;
            return View(dto);
        }

        var result = await _userService.ResetPasswordAsync(dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var user = await _userService.GetByIdAsync(dto.UserId);
            ViewBag.UserName = user?.FullName;
            ViewBag.UserEmail = user?.Email;
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
