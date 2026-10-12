using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<UserDto>> GetUsersAsync(string? search)
    {
        var usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            usersQuery = usersQuery.Where(u => u.FullName.ToLower().Contains(s)
                                             || u.Email!.ToLower().Contains(s)
                                             || (u.Department != null && u.Department.ToLower().Contains(s)));
        }

        var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            result.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? "None",
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = user.AccessFailedCount,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            });
        }

        return result;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = await _userManager.IsLockedOutAsync(user);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Department = user.Department,
            Role = roles.FirstOrDefault() ?? "None",
            IsActive = user.IsActive,
            IsLockedOut = isLocked,
            AccessFailedCount = user.AccessFailedCount,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate
        };
    }

    public async Task<(bool Success, string Message, string Id)> CreateUserAsync(UserCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, "Email is required.", string.Empty);

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (false, "Full Name is required.", string.Empty);

        var existing = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existing != null)
            return (false, "A user with this email address already exists.", string.Empty);

        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return (false, $"Role '{dto.Role}' does not exist.", string.Empty);

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim().ToLower(),
            Email = dto.Email.Trim().ToLower(),
            FullName = dto.FullName.Trim(),
            Department = dto.Department?.Trim(),
            IsActive = dto.IsActive,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"User creation failed: {errors}", string.Empty);
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        await _audit.LogAsync(AuditActions.Create, "User", user.Id, null,
            $"User '{user.FullName}' ({user.Email}) created with role '{dto.Role}'.");

        return (true, "User created successfully.", user.Id);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(UserEditDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
            return (false, "User not found.");

        // Prevent self-lockout or revoking own admin status
        if (user.Id == _currentUser.UserId)
        {
            if (!dto.IsActive)
                return (false, "You cannot deactivate your own account.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(AppRoles.Admin) && dto.Role != AppRoles.Admin)
                return (false, "You cannot revoke Admin role from your own account.");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim().ToLower();
        user.UserName = dto.Email.Trim().ToLower();
        user.Department = dto.Department?.Trim();
        user.IsActive = dto.IsActive;

        if (dto.UnlockAccount)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _audit.LogAsync(AuditActions.Unlock, "User", user.Id, null, $"Account unlocked by administrator.");
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return (false, $"Update failed: {errors}");
        }

        // Update role if changed
        var currentRolesList = await _userManager.GetRolesAsync(user);
        var oldRole = currentRolesList.FirstOrDefault() ?? "None";
        if (oldRole != dto.Role && await _roleManager.RoleExistsAsync(dto.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRolesList);
            await _userManager.AddToRoleAsync(user, dto.Role);

            await _audit.LogAsync(AuditActions.RoleChange, "User", user.Id, oldRole, dto.Role);
        }

        await _audit.LogAsync(AuditActions.Update, "User", user.Id, null, $"User '{user.FullName}' updated.");

        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> ToggleStatusAsync(string id)
    {
        if (id == _currentUser.UserId)
            return (false, "You cannot deactivate your own account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found.");

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var action = user.IsActive ? "Activated" : "Deactivated";
        await _audit.LogAsync(AuditActions.StatusChange, "User", user.Id, null, $"Account {action}");

        return (true, $"User account {action} successfully.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return (false, "User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Password reset failed: {errors}");
        }

        await _audit.LogAsync(AuditActions.PasswordReset, "User", user.Id, null, "Password reset by administrator.");

        return (true, "Password reset successfully.");
    }

    public async Task<List<ApplicationUser>> GetSalesExecutivesAsync()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        var execs = new List<ApplicationUser>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (roles.Contains(AppRoles.SalesExecutive) || roles.Contains(AppRoles.Manager) || roles.Contains(AppRoles.Admin))
            {
                execs.Add(u);
            }
        }

        return execs;
    }
}
