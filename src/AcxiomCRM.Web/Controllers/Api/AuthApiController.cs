using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid login request", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim())
                   ?? await _userManager.FindByNameAsync(model.Email.Trim());

        if (user == null)
        {
            await _audit.LogAsync(AuditActions.FailedLogin, "Authentication", model.Email, null, "API failed login: user not found.");
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Unauthorized(ApiResponse<object>.Fail("Account is deactivated. Contact an administrator."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            await _audit.LogAsync(AuditActions.Login, "Authentication", user.Id, null, $"API login successful: {user.Email}");

            return Ok(ApiResponse<object>.Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            }, "Authentication successful."));
        }

        if (result.IsLockedOut)
        {
            await _audit.LogAsync(AuditActions.Lockout, "Authentication", user.Id, null, "API user locked out.");
            return StatusCode(423, ApiResponse<object>.Fail("Account is temporarily locked due to multiple failed login attempts."));
        }

        await _audit.LogAsync(AuditActions.FailedLogin, "Authentication", user.Id, null, "API invalid password.");
        return Unauthorized(ApiResponse<object>.Fail("Invalid email or password."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();

        if (userId != null)
        {
            await _audit.LogAsync(AuditActions.Logout, "Authentication", userId, null, "API user logged out.");
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully."));
    }
}
