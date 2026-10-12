using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class FollowUpService : IFollowUpService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public FollowUpService(AppDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<FollowUp> GetScopedQuery()
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(f => f.AssignedTo == _currentUser.UserId);
        }

        return query;
    }

    public async Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(string? search, string? status, string? type, DateTime? date, int pageIndex, int pageSize)
    {
        var query = GetScopedQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f => f.Subject.ToLower().Contains(s)
                                  || (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(s))
                                  || (f.Lead != null && f.Lead.LeadName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FollowUpStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(f => f.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<FollowUpType>(type, true, out var parsedType))
        {
            query = query.Where(f => f.FollowUpType == parsedType);
        }

        if (date.HasValue)
        {
            var d = date.Value.Date;
            query = query.Where(f => f.FollowUpDate.Date == d);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(f => f.FollowUpDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned",
                CreatedDate = f.CreatedDate,
                CompletedDate = f.CompletedDate
            })
            .ToListAsync();

        return new PagedResult<FollowUpDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<FollowUpDto?> GetByIdAsync(int id)
    {
        var f = await GetScopedQuery().FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (f == null) return null;

        return new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo,
            AssignedUserName = f.AssignedUser?.FullName ?? "Unassigned",
            CreatedDate = f.CreatedDate,
            CompletedDate = f.CompletedDate
        };
    }

    public async Task<FollowUpCreateUpdateDto?> GetForEditAsync(int id)
    {
        var f = await GetScopedQuery().FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (f == null) return null;

        return new FollowUpCreateUpdateDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            LeadId = f.LeadId,
            OpportunityId = f.OpportunityId,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo
        };
    }

    public async Task<(bool Success, string Message, int Id)> CreateAsync(FollowUpCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return (false, "Subject is required.", 0);

        if (string.IsNullOrWhiteSpace(dto.AssignedTo))
            return (false, "Assigned user is required.", 0);

        // Date business rule: cannot be earlier than today for planned follow-up
        if (dto.Status == FollowUpStatus.Planned && dto.FollowUpDate.Date < DateTime.Today)
            return (false, "Follow-up date cannot be earlier than today.", 0);

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject.Trim(),
            Remarks = dto.Remarks?.Trim(),
            Status = dto.Status,
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Create, "FollowUp", followUp.FollowUpId.ToString(), null,
            $"Follow-up scheduled: '{followUp.Subject}' on {followUp.FollowUpDate:d}");

        return (true, "Follow-up scheduled successfully.", followUp.FollowUpId);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(FollowUpCreateUpdateDto dto)
    {
        var followUp = await GetScopedQuery().FirstOrDefaultAsync(f => f.FollowUpId == dto.FollowUpId);
        if (followUp == null)
            return (false, "Follow-up not found or unauthorized.");

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return (false, "Subject is required.");

        // If rescheduling a planned follow-up
        if (dto.Status == FollowUpStatus.Planned && dto.FollowUpDate.Date < DateTime.Today && dto.FollowUpDate.Date != followUp.FollowUpDate.Date)
            return (false, "Rescheduled follow-up date cannot be earlier than today.");

        var oldInfo = $"{followUp.Subject} ({followUp.FollowUpDate:d}, {followUp.Status})";

        followUp.CustomerId = dto.CustomerId;
        followUp.LeadId = dto.LeadId;
        followUp.OpportunityId = dto.OpportunityId;
        followUp.FollowUpDate = dto.FollowUpDate;
        followUp.FollowUpType = dto.FollowUpType;
        followUp.Subject = dto.Subject.Trim();
        followUp.Remarks = dto.Remarks?.Trim();
        followUp.Status = dto.Status;

        if (!_currentUser.IsSalesExecutive)
        {
            followUp.AssignedTo = dto.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Update, "FollowUp", followUp.FollowUpId.ToString(), oldInfo,
            $"{followUp.Subject} ({followUp.FollowUpDate:d}, {followUp.Status})");

        return (true, "Follow-up updated successfully.");
    }

    public async Task<(bool Success, string Message)> CompleteAsync(int id, string? remarks)
    {
        var followUp = await GetScopedQuery().FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null)
            return (false, "Follow-up not found or unauthorized.");

        followUp.Status = FollowUpStatus.Completed;
        followUp.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrEmpty(followUp.Remarks)
                ? $"Completed: {remarks}"
                : $"{followUp.Remarks}\nCompleted: {remarks}";
        }

        // Also create an Activity record corresponding to completed follow-up
        var activity = new Activity
        {
            ActivityType = followUp.FollowUpType switch
            {
                FollowUpType.Call => ActivityType.Call,
                FollowUpType.Meeting => ActivityType.Meeting,
                FollowUpType.Email => ActivityType.Email,
                _ => ActivityType.Task
            },
            Subject = $"Completed Follow-up: {followUp.Subject}",
            Description = followUp.Remarks,
            ActivityDate = DateTime.UtcNow,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            AssignedTo = followUp.AssignedTo,
            Status = ActivityStatus.Completed,
            CreatedDate = DateTime.UtcNow
        };
        _context.Activities.Add(activity);

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.StatusChange, "FollowUp", followUp.FollowUpId.ToString(),
            "Planned", "Completed");

        return (true, "Follow-up marked as Completed.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id, string? remarks)
    {
        var followUp = await GetScopedQuery().FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null)
            return (false, "Follow-up not found or unauthorized.");

        followUp.Status = FollowUpStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = $"{followUp.Remarks}\nCancelled: {remarks}".Trim();
        }

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.StatusChange, "FollowUp", followUp.FollowUpId.ToString(),
            "Planned", "Cancelled");

        return (true, "Follow-up cancelled.");
    }

    public async Task<(bool Success, string Message)> MarkMissedAsync(int id)
    {
        var followUp = await GetScopedQuery().FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null)
            return (false, "Follow-up not found or unauthorized.");

        followUp.Status = FollowUpStatus.Missed;
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.StatusChange, "FollowUp", followUp.FollowUpId.ToString(),
            "Planned", "Missed");

        return (true, "Follow-up marked as Missed.");
    }

    public async Task<List<FollowUpDto>> GetUpcomingAsync(int count = 5)
    {
        var today = DateTime.Today;
        return await GetScopedQuery()
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date >= today)
            .OrderBy(f => f.FollowUpDate)
            .Take(count)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned"
            })
            .ToListAsync();
    }

    public async Task<List<FollowUpDto>> GetOverdueAsync(int count = 5)
    {
        var today = DateTime.Today;
        return await GetScopedQuery()
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today)
            .OrderBy(f => f.FollowUpDate)
            .Take(count)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned"
            })
            .ToListAsync();
    }
}
