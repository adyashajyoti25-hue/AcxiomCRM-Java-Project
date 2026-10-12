using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class LeadService : ILeadService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public LeadService(AppDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Lead> GetScopedQuery()
    {
        var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(l => l.AssignedTo == _currentUser.UserId);
        }

        return query;
    }

    public async Task<PagedResult<LeadDto>> GetLeadsAsync(string? search, string? status, string? assignedTo, int pageIndex, int pageSize)
    {
        var query = GetScopedQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(s)
                                  || l.Email.ToLower().Contains(s)
                                  || l.Phone.Contains(s)
                                  || (l.CompanyName != null && l.CompanyName.ToLower().Contains(s))
                                  || l.LeadCode.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(l => l.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo) && !_currentUser.IsSalesExecutive)
        {
            query = query.Where(l => l.AssignedTo == assignedTo);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(l => l.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                ModifiedDate = l.ModifiedDate,
                CreatedBy = l.CreatedBy,
                AssignedTo = l.AssignedTo,
                AssignedUserName = l.AssignedUser != null ? l.AssignedUser.FullName : "Unassigned",
                ConvertedCustomerId = l.ConvertedCustomerId,
                ConvertedOpportunityId = l.ConvertedOpportunityId,
                Notes = l.Notes
            })
            .ToListAsync();

        return new PagedResult<LeadDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<LeadDto?> GetByIdAsync(int id)
    {
        var lead = await GetScopedQuery().FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return null;

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            ModifiedDate = lead.ModifiedDate,
            CreatedBy = lead.CreatedBy,
            AssignedTo = lead.AssignedTo,
            AssignedUserName = lead.AssignedUser?.FullName ?? "Unassigned",
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedOpportunityId = lead.ConvertedOpportunityId,
            Notes = lead.Notes
        };
    }

    public async Task<LeadCreateUpdateDto?> GetForEditAsync(int id)
    {
        var l = await GetScopedQuery().FirstOrDefaultAsync(x => x.LeadId == id);
        if (l == null) return null;

        return new LeadCreateUpdateDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            AssignedTo = l.AssignedTo,
            Notes = l.Notes
        };
    }

    public async Task<(bool Success, string Message, int Id)> CreateAsync(LeadCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LeadName))
            return (false, "Lead Name is required.", 0);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, "Enter a valid email address.", 0);

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return (false, "Enter a valid phone number.", 0);

        if (dto.ExpectedValue < 0)
            return (false, "Expected Value must be greater than or equal to 0.", 0);

        var lastId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
        var code = $"LEAD-{lastId + 2001:D4}";

        var lead = new Lead
        {
            LeadCode = code,
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            AssignedTo = dto.AssignedTo ?? _currentUser.UserId,
            Notes = dto.Notes?.Trim()
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Create, "Lead", lead.LeadCode, null, $"Lead '{lead.LeadName}' created with expected value {lead.ExpectedValue:C}.");

        return (true, "Lead created successfully.", lead.LeadId);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(LeadCreateUpdateDto dto)
    {
        var lead = await GetScopedQuery().FirstOrDefaultAsync(l => l.LeadId == dto.LeadId);
        if (lead == null)
            return (false, "Lead not found or unauthorized.");

        if (lead.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot be modified.");

        if (string.IsNullOrWhiteSpace(dto.LeadName))
            return (false, "Lead Name is required.");

        if (dto.ExpectedValue < 0)
            return (false, "Expected Value cannot be negative.");

        var oldInfo = $"{lead.LeadName} [{lead.Status}]";

        lead.LeadName = dto.LeadName.Trim();
        lead.Email = dto.Email.Trim().ToLower();
        lead.Phone = dto.Phone.Trim();
        lead.CompanyName = dto.CompanyName?.Trim();
        lead.Source = dto.Source;
        lead.Status = dto.Status;
        lead.Priority = dto.Priority;
        lead.ExpectedValue = dto.ExpectedValue;
        lead.ModifiedDate = DateTime.UtcNow;

        if (!_currentUser.IsSalesExecutive)
        {
            lead.AssignedTo = dto.AssignedTo;
        }

        lead.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Update, "Lead", lead.LeadCode, oldInfo, $"{lead.LeadName} [{lead.Status}]");

        return (true, "Lead updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var lead = await GetScopedQuery().FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null)
            return (false, "Lead not found or unauthorized.");

        if (lead.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot be deleted.");

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Delete, "Lead", lead.LeadCode, lead.LeadName, null);

        return (true, "Lead deleted successfully.");
    }

    public async Task<(bool Success, string Message)> ConvertLeadAsync(LeadConvertDto dto)
    {
        var lead = await GetScopedQuery().FirstOrDefaultAsync(l => l.LeadId == dto.LeadId);
        if (lead == null)
            return (false, "Lead not found or unauthorized.");

        if (lead.Status == LeadStatus.Converted)
            return (false, "This lead has already been converted.");

        if (lead.Status == LeadStatus.Unqualified || lead.Status == LeadStatus.Lost)
            return (false, $"A lead with status '{lead.Status}' cannot be converted. Must be in Qualified status.");

        if (dto.CreateOpportunity)
        {
            if (dto.OpportunityAmount.HasValue && dto.OpportunityAmount.Value <= 0)
                return (false, "Opportunity Amount must be greater than 0.");

            if (dto.ExpectedCloseDate.HasValue && dto.ExpectedCloseDate.Value.Date < DateTime.Today)
                return (false, "Expected Close Date cannot be in the past.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check if Customer already exists with this email or phone
            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);

            if (customer == null)
            {
                var lastCustId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
                var custCode = $"CUST-{lastCustId + 1001:D4}";

                customer = new Customer
                {
                    CustomerCode = custCode,
                    CustomerName = lead.CompanyName ?? lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName ?? lead.LeadName,
                    Status = CustomerStatus.Active,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId,
                    AssignedTo = dto.AssignedTo ?? lead.AssignedTo ?? _currentUser.UserId,
                    Notes = $"Converted from Lead {lead.LeadCode}."
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            lead.ConvertedCustomerId = customer.CustomerId;

            // 2. Optionally create Opportunity
            if (dto.CreateOpportunity)
            {
                var opp = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(dto.OpportunityName)
                        ? $"{customer.CustomerName} - New Business"
                        : dto.OpportunityName.Trim(),
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = dto.OpportunityAmount ?? (lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m),
                    Stage = OpportunityStage.Qualification,
                    Probability = 25,
                    ExpectedCloseDate = dto.ExpectedCloseDate ?? DateTime.Today.AddDays(30),
                    Status = OpportunityStatus.Open,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId,
                    AssignedTo = dto.AssignedTo ?? lead.AssignedTo ?? _currentUser.UserId,
                    Notes = $"Opportunity generated during conversion of Lead {lead.LeadCode}. {dto.Remarks}"
                };

                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();

                lead.ConvertedOpportunityId = opp.OpportunityId;
            }

            // 3. Mark Lead as Converted
            lead.Status = LeadStatus.Converted;
            lead.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync(AuditActions.Convert, "Lead", lead.LeadCode,
                $"Status: Qualified",
                $"Converted to Customer '{customer.CustomerName}' (ID: {customer.CustomerId})");

            await transaction.CommitAsync();

            return (true, "Lead successfully converted to Customer and Opportunity.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Conversion failed: {ex.Message}");
        }
    }

    public async Task<List<LeadDto>> GetAllLookupAsync()
    {
        return await GetScopedQuery()
            .Where(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost)
            .OrderBy(l => l.LeadName)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName
            })
            .ToListAsync();
    }
}
