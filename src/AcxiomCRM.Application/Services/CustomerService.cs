using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CustomerService(AppDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    private IQueryable<Customer> GetScopedQuery()
    {
        var query = _context.Customers.Include(c => c.AssignedUser).AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            query = query.Where(c => c.AssignedTo == _currentUser.UserId);
        }

        return query;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(string? search, string? status, int pageIndex, int pageSize)
    {
        var query = GetScopedQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(s)
                                  || c.Email.ToLower().Contains(s)
                                  || c.Phone.Contains(s)
                                  || (c.CompanyName != null && c.CompanyName.ToLower().Contains(s))
                                  || c.CustomerCode.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(c => c.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var rawItems = await query.OrderByDescending(c => c.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.CustomerId,
                c.CustomerCode,
                c.CustomerName,
                c.Email,
                c.Phone,
                c.CompanyName,
                c.Address,
                c.City,
                c.State,
                c.Status,
                c.CreatedDate,
                c.ModifiedDate,
                c.CreatedBy,
                c.AssignedTo,
                AssignedUserName = c.AssignedUser != null ? c.AssignedUser.FullName : "Unassigned",
                c.Notes,
                ActiveOpportunitiesCount = c.Opportunities.Count(o => o.Status == OpportunityStatus.Open),
                WonAmounts = c.Opportunities.Where(o => o.Status == OpportunityStatus.Won).Select(o => o.Amount).ToList()
            })
            .ToListAsync();

        var items = rawItems.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            ModifiedDate = c.ModifiedDate,
            CreatedBy = c.CreatedBy,
            AssignedTo = c.AssignedTo,
            AssignedUserName = c.AssignedUserName,
            Notes = c.Notes,
            ActiveOpportunitiesCount = c.ActiveOpportunitiesCount,
            TotalWonValue = c.WonAmounts.Sum()
        }).ToList();

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await GetScopedQuery()
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            ModifiedDate = customer.ModifiedDate,
            CreatedBy = customer.CreatedBy,
            AssignedTo = customer.AssignedTo,
            AssignedUserName = customer.AssignedUser?.FullName ?? "Unassigned",
            Notes = customer.Notes,
            ActiveOpportunitiesCount = customer.Opportunities.Count(o => o.Status == OpportunityStatus.Open),
            TotalWonValue = customer.Opportunities.Where(o => o.Status == OpportunityStatus.Won).Sum(o => o.Amount)
        };
    }

    public async Task<CustomerCreateUpdateDto?> GetForEditAsync(int id)
    {
        var c = await GetScopedQuery().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null) return null;

        return new CustomerCreateUpdateDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            AssignedTo = c.AssignedTo,
            Notes = c.Notes
        };
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var q = _context.Customers.Where(c => c.Email.ToLower() == email.Trim().ToLower());
        if (excludeId.HasValue)
            q = q.Where(c => c.CustomerId != excludeId.Value);

        return await q.AnyAsync();
    }

    public async Task<bool> PhoneExistsAsync(string phone, int? excludeId = null)
    {
        var q = _context.Customers.Where(c => c.Phone == phone.Trim());
        if (excludeId.HasValue)
            q = q.Where(c => c.CustomerId != excludeId.Value);

        return await q.AnyAsync();
    }

    public async Task<(bool Success, string Message, int Id)> CreateAsync(CustomerCreateUpdateDto dto)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return (false, "Customer Name is required.", 0);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return (false, "Enter a valid email address.", 0);

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return (false, "Enter a valid phone number.", 0);

        if (await EmailExistsAsync(dto.Email))
            return (false, "A customer with this email address already exists.", 0);

        if (await PhoneExistsAsync(dto.Phone))
            return (false, "A customer with this phone number already exists.", 0);

        // Generate CustomerCode
        var lastId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
        var code = $"CUST-{lastId + 1001:D4}";

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            AssignedTo = dto.AssignedTo ?? _currentUser.UserId,
            Notes = dto.Notes?.Trim()
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Create, "Customer", customer.CustomerCode, null, $"Customer '{customer.CustomerName}' created.");

        return (true, "Customer created successfully.", customer.CustomerId);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(CustomerCreateUpdateDto dto)
    {
        var customer = await GetScopedQuery().FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId);
        if (customer == null)
            return (false, "Customer not found or unauthorized.");

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            return (false, "Customer Name is required.");

        if (await EmailExistsAsync(dto.Email, dto.CustomerId))
            return (false, "Another customer is already registered with this email address.");

        if (await PhoneExistsAsync(dto.Phone, dto.CustomerId))
            return (false, "Another customer is already registered with this phone number.");

        var oldInfo = $"{customer.CustomerName} ({customer.Email}, {customer.Phone})";

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = dto.Email.Trim().ToLower();
        customer.Phone = dto.Phone.Trim();
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;
        customer.ModifiedDate = DateTime.UtcNow;

        if (!_currentUser.IsSalesExecutive || string.IsNullOrEmpty(customer.AssignedTo))
        {
            customer.AssignedTo = dto.AssignedTo;
        }

        customer.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Update, "Customer", customer.CustomerCode, oldInfo, $"{customer.CustomerName} ({customer.Email}, {customer.Phone})");

        return (true, "Customer updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var customer = await GetScopedQuery()
            .Include(c => c.Opportunities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
            return (false, "Customer not found or unauthorized.");

        // If customer has open opportunities, deactivate instead of hard delete
        if (customer.Opportunities.Any(o => o.Status == OpportunityStatus.Open))
        {
            customer.Status = CustomerStatus.Inactive;
            customer.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(AuditActions.Deactivate, "Customer", customer.CustomerCode, null, $"Customer '{customer.CustomerName}' deactivated due to active opportunities.");
            return (true, "Customer deactivated because there are active opportunities.");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditActions.Delete, "Customer", customer.CustomerCode, customer.CustomerName, null);

        return (true, "Customer deleted successfully.");
    }

    public async Task<List<CustomerDto>> GetAllLookupAsync()
    {
        return await GetScopedQuery()
            .Where(c => c.Status == CustomerStatus.Active)
            .OrderBy(c => c.CustomerName)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName
            })
            .ToListAsync();
    }
}
