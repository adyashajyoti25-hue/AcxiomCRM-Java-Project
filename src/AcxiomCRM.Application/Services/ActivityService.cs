using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class ActivityService : IActivityService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public ActivityService(AppDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Activity> GetScopedQuery()
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(a => a.AssignedTo == _currentUser.UserId);
        }

        return query;
    }

    public async Task<PagedResult<ActivityDto>> GetActivitiesAsync(string? search, string? type, string? status, int pageIndex, int pageSize)
    {
        var query = GetScopedQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(s)
                                  || (a.Description != null && a.Description.ToLower().Contains(s))
                                  || (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(s))
                                  || (a.Lead != null && a.Lead.LeadName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ActivityType>(type, true, out var parsedType))
        {
            query = query.Where(a => a.ActivityType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ActivityStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(a => a.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.ActivityDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer != null ? a.Customer.CustomerName : null,
                LeadId = a.LeadId,
                LeadName = a.Lead != null ? a.Lead.LeadName : null,
                AssignedTo = a.AssignedTo,
                AssignedUserName = a.AssignedUser != null ? a.AssignedUser.FullName : "Unassigned",
                Status = a.Status,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync();

        return new PagedResult<ActivityDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<ActivityDto?> GetByIdAsync(int id)
    {
        var a = await GetScopedQuery().FirstOrDefaultAsync(x => x.ActivityId == id);
        if (a == null) return null;

        return new ActivityDto
        {
            ActivityId = a.ActivityId,
            ActivityType = a.ActivityType,
            Subject = a.Subject,
            Description = a.Description,
            ActivityDate = a.ActivityDate,
            CustomerId = a.CustomerId,
            CustomerName = a.Customer?.CustomerName,
            LeadId = a.LeadId,
            LeadName = a.Lead?.LeadName,
            AssignedTo = a.AssignedTo,
            AssignedUserName = a.AssignedUser?.FullName ?? "Unassigned",
            Status = a.Status,
            CreatedDate = a.CreatedDate
        };
    }

    public async Task<(bool Success, string Message, int Id)> CreateAsync(ActivityCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return (false, "Subject is required.", 0);

        if (string.IsNullOrWhiteSpace(dto.AssignedTo))
            return (false, "Assigned user is required.", 0);

        var activity = new Activity
        {
            ActivityType = dto.ActivityType,
            Subject = dto.Subject.Trim(),
            Description = dto.Description?.Trim(),
            ActivityDate = dto.ActivityDate,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            AssignedTo = dto.AssignedTo,
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Create, "Activity", activity.ActivityId.ToString(), null,
            $"Activity logged: {activity.ActivityType} - '{activity.Subject}'");

        return (true, "Activity created successfully.", activity.ActivityId);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(ActivityCreateUpdateDto dto)
    {
        var activity = await GetScopedQuery().FirstOrDefaultAsync(a => a.ActivityId == dto.ActivityId);
        if (activity == null)
            return (false, "Activity not found or unauthorized.");

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return (false, "Subject is required.");

        var oldInfo = $"{activity.ActivityType}: {activity.Subject}";

        activity.ActivityType = dto.ActivityType;
        activity.Subject = dto.Subject.Trim();
        activity.Description = dto.Description?.Trim();
        activity.ActivityDate = dto.ActivityDate;
        activity.CustomerId = dto.CustomerId;
        activity.LeadId = dto.LeadId;
        activity.Status = dto.Status;

        if (!_currentUser.IsSalesExecutive)
        {
            activity.AssignedTo = dto.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Update, "Activity", activity.ActivityId.ToString(), oldInfo,
            $"{activity.ActivityType}: {activity.Subject}");

        return (true, "Activity updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var activity = await GetScopedQuery().FirstOrDefaultAsync(a => a.ActivityId == id);
        if (activity == null)
            return (false, "Activity not found or unauthorized.");

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Delete, "Activity", activity.ActivityId.ToString(), activity.Subject, null);

        return (true, "Activity deleted successfully.");
    }
}
