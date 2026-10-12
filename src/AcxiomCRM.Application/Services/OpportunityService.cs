using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class OpportunityService : IOpportunityService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public OpportunityService(AppDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Opportunity> GetScopedQuery()
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(o => o.AssignedTo == _currentUser.UserId);
        }

        return query;
    }

    public async Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(string? search, string? stage, string? status, int pageIndex, int pageSize)
    {
        var query = GetScopedQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(s)
                                  || (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s))
                                  || (o.Customer != null && o.Customer.CustomerCode.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<OpportunityStage>(stage, true, out var parsedStage))
        {
            query = query.Where(o => o.Stage == parsedStage);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OpportunityStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(o => o.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                ModifiedDate = o.ModifiedDate,
                CreatedBy = o.CreatedBy,
                AssignedTo = o.AssignedTo,
                AssignedUserName = o.AssignedUser != null ? o.AssignedUser.FullName : "Unassigned",
                Notes = o.Notes
            })
            .ToListAsync();

        return new PagedResult<OpportunityDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<OpportunityDto?> GetByIdAsync(int id)
    {
        var o = await GetScopedQuery().FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (o == null) return null;

        return new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName ?? "Unknown",
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            ModifiedDate = o.ModifiedDate,
            CreatedBy = o.CreatedBy,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName ?? "Unassigned",
            Notes = o.Notes
        };
    }

    public async Task<OpportunityCreateUpdateDto?> GetForEditAsync(int id)
    {
        var o = await GetScopedQuery().FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (o == null) return null;

        return new OpportunityCreateUpdateDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedTo = o.AssignedTo,
            Notes = o.Notes
        };
    }

    public async Task<(bool Success, string Message, int Id)> CreateAsync(OpportunityCreateUpdateDto dto)
    {
        // 1. Mandatory Business Validations
        if (string.IsNullOrWhiteSpace(dto.OpportunityName))
            return (false, "Opportunity Name is required.", 0);

        if (dto.CustomerId <= 0)
            return (false, "A valid Customer must be selected.", 0);

        if (dto.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", 0);

        if (dto.Probability < 0 || dto.Probability > 100)
            return (false, "Probability must be between 0 and 100.", 0);

        if (dto.Status == OpportunityStatus.Open && dto.ExpectedCloseDate.Date < DateTime.Today)
            return (false, "Expected Close Date cannot be in the past.", 0);

        // Verify Customer exists
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == dto.CustomerId);
        if (!customerExists)
            return (false, "The selected customer does not exist.", 0);

        // Align status based on stage
        var status = dto.Stage switch
        {
            OpportunityStage.Won => OpportunityStatus.Won,
            OpportunityStage.Lost => OpportunityStatus.Lost,
            _ => OpportunityStatus.Open
        };

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            AssignedTo = dto.AssignedTo ?? _currentUser.UserId,
            Notes = dto.Notes?.Trim()
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Create, "Opportunity", opp.OpportunityId.ToString(), null,
            $"Opportunity '{opp.OpportunityName}' created with Amount {opp.Amount:C} and Stage {opp.Stage}.");

        return (true, "Opportunity created successfully.", opp.OpportunityId);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(OpportunityCreateUpdateDto dto)
    {
        var opp = await GetScopedQuery().FirstOrDefaultAsync(o => o.OpportunityId == dto.OpportunityId);
        if (opp == null)
            return (false, "Opportunity not found or unauthorized.");

        if (string.IsNullOrWhiteSpace(dto.OpportunityName))
            return (false, "Opportunity Name is required.");

        if (dto.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.");

        if (dto.Probability < 0 || dto.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        var newStatus = dto.Stage switch
        {
            OpportunityStage.Won => OpportunityStatus.Won,
            OpportunityStage.Lost => OpportunityStatus.Lost,
            _ => OpportunityStatus.Open
        };

        if (newStatus == OpportunityStatus.Open && dto.ExpectedCloseDate.Date < DateTime.Today)
            return (false, "Expected Close Date cannot be in the past.");

        var oldInfo = $"{opp.OpportunityName} - {opp.Stage} ({opp.Amount:C}, {opp.Probability}%)";

        opp.OpportunityName = dto.OpportunityName.Trim();
        opp.CustomerId = dto.CustomerId;
        opp.LeadId = dto.LeadId;
        opp.Amount = dto.Amount;
        opp.Stage = dto.Stage;
        opp.Probability = dto.Probability;
        opp.ExpectedCloseDate = dto.ExpectedCloseDate;
        opp.Status = newStatus;
        opp.ModifiedDate = DateTime.UtcNow;

        if (!_currentUser.IsSalesExecutive)
        {
            opp.AssignedTo = dto.AssignedTo;
        }

        opp.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Update, "Opportunity", opp.OpportunityId.ToString(), oldInfo,
            $"{opp.OpportunityName} - {opp.Stage} ({opp.Amount:C}, {opp.Probability}%)");

        return (true, "Opportunity updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var opp = await GetScopedQuery().FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp == null)
            return (false, "Opportunity not found or unauthorized.");

        _context.Opportunities.Remove(opp);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Delete, "Opportunity", opp.OpportunityId.ToString(), opp.OpportunityName, null);

        return (true, "Opportunity deleted successfully.");
    }
}
