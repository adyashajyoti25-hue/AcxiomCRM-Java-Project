using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string entityName, string? recordId, string? oldValue, string? newValue)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = _currentUser.UserId,
                UserEmail = _currentUser.Email ?? "system@acxiomcrm.com",
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedDate = DateTime.UtcNow,
                IpAddress = _currentUser.IpAddress ?? "127.0.0.1"
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Fail safely without blocking critical business operations
        }
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(a => a.UserId == filter.UserId);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(a => a.EntityName == filter.EntityName);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.CreatedDate <= filter.ToDate.Value.AddDays(1).AddTicks(-1));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityName = a.EntityName,
                RecordId = a.RecordId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedDate = a.CreatedDate,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }
}
