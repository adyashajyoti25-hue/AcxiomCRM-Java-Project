using System.Text;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReportService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomerReportAsync(ReportFilterDto filter)
    {
        var q = _context.Customers.Include(c => c.AssignedUser).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            q = q.Where(c => c.AssignedTo == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.Trim().ToLower();
            q = q.Where(c => c.CustomerName.ToLower().Contains(s) || c.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<CustomerStatus>(filter.Status, true, out var st))
            q = q.Where(c => c.Status == st);

        if (!string.IsNullOrWhiteSpace(filter.AssignedTo) && !_currentUser.IsSalesExecutive)
            q = q.Where(c => c.AssignedTo == filter.AssignedTo);

        if (filter.FromDate.HasValue)
            q = q.Where(c => c.CreatedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            q = q.Where(c => c.CreatedDate <= filter.ToDate.Value.AddDays(1).AddTicks(-1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.CreatedDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedUserName = c.AssignedUser != null ? c.AssignedUser.FullName : "Unassigned"
            })
            .ToListAsync();

        return new PagedResult<CustomerDto> { Items = items, TotalCount = total, PageIndex = filter.PageIndex, PageSize = filter.PageSize };
    }

    public async Task<PagedResult<LeadDto>> GetLeadReportAsync(ReportFilterDto filter)
    {
        var q = _context.Leads.Include(l => l.AssignedUser).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            q = q.Where(l => l.AssignedTo == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.Trim().ToLower();
            q = q.Where(l => l.LeadName.ToLower().Contains(s) || l.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<LeadStatus>(filter.Status, true, out var st))
            q = q.Where(l => l.Status == st);

        if (filter.FromDate.HasValue)
            q = q.Where(l => l.CreatedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            q = q.Where(l => l.CreatedDate <= filter.ToDate.Value.AddDays(1).AddTicks(-1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(l => l.CreatedDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
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
                AssignedUserName = l.AssignedUser != null ? l.AssignedUser.FullName : "Unassigned",
                ConvertedCustomerId = l.ConvertedCustomerId
            })
            .ToListAsync();

        return new PagedResult<LeadDto> { Items = items, TotalCount = total, PageIndex = filter.PageIndex, PageSize = filter.PageSize };
    }

    public async Task<PagedResult<FollowUpDto>> GetFollowUpReportAsync(ReportFilterDto filter)
    {
        var q = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            q = q.Where(f => f.AssignedTo == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<FollowUpStatus>(filter.Status, true, out var st))
            q = q.Where(f => f.Status == st);

        if (filter.FromDate.HasValue)
            q = q.Where(f => f.FollowUpDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            q = q.Where(f => f.FollowUpDate <= filter.ToDate.Value.AddDays(1).AddTicks(-1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(f => f.FollowUpDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedUserName = f.AssignedUser != null ? f.AssignedUser.FullName : "Unassigned",
                CompletedDate = f.CompletedDate
            })
            .ToListAsync();

        return new PagedResult<FollowUpDto> { Items = items, TotalCount = total, PageIndex = filter.PageIndex, PageSize = filter.PageSize };
    }

    public async Task<PagedResult<OpportunityDto>> GetOpportunityReportAsync(ReportFilterDto filter)
    {
        var q = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            q = q.Where(o => o.AssignedTo == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(filter.Stage) && Enum.TryParse<OpportunityStage>(filter.Stage, true, out var stg))
            q = q.Where(o => o.Stage == stg);

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<OpportunityStatus>(filter.Status, true, out var st))
            q = q.Where(o => o.Status == st);

        if (filter.FromDate.HasValue)
            q = q.Where(o => o.CreatedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            q = q.Where(o => o.CreatedDate <= filter.ToDate.Value.AddDays(1).AddTicks(-1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedUserName = o.AssignedUser != null ? o.AssignedUser.FullName : "Unassigned"
            })
            .ToListAsync();

        return new PagedResult<OpportunityDto> { Items = items, TotalCount = total, PageIndex = filter.PageIndex, PageSize = filter.PageSize };
    }

    public async Task<object> GetPipelineReportAsync()
    {
        var oppsQ = _context.Opportunities.Include(o => o.AssignedUser).Include(o => o.Customer).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            oppsQ = oppsQ.Where(o => o.AssignedTo == _currentUser.UserId);

        var list = await oppsQ.ToListAsync();

        var stageSummary = list.GroupBy(o => o.Stage)
            .Select(g => new
            {
                Stage = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedValue)
            })
            .ToList();

        var ownerSummary = list.GroupBy(o => o.AssignedUser?.FullName ?? "Unassigned")
            .Select(g => new
            {
                Owner = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedValue)
            })
            .ToList();

        return new
        {
            TotalPipeline = list.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount),
            TotalWeightedPipeline = list.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.WeightedValue),
            StageSummary = stageSummary,
            OwnerSummary = ownerSummary
        };
    }

    public async Task<object> GetConversionReportAsync()
    {
        var leadsQ = _context.Leads.Include(l => l.AssignedUser).AsNoTracking().AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
            leadsQ = leadsQ.Where(l => l.AssignedTo == _currentUser.UserId);

        var totalLeads = await leadsQ.CountAsync();
        var convertedLeads = await leadsQ.CountAsync(l => l.Status == LeadStatus.Converted);
        var lostLeads = await leadsQ.CountAsync(l => l.Status == LeadStatus.Lost);
        var activeLeads = totalLeads - convertedLeads - lostLeads;
        var conversionRate = totalLeads > 0 ? Math.Round((decimal)convertedLeads / totalLeads * 100m, 1) : 0;

        var sourceConversions = await leadsQ.GroupBy(l => l.Source)
            .Select(g => new
            {
                Source = g.Key.ToString(),
                Total = g.Count(),
                Converted = g.Count(x => x.Status == LeadStatus.Converted),
                ConversionRate = g.Count() > 0 ? Math.Round((decimal)g.Count(x => x.Status == LeadStatus.Converted) / g.Count() * 100m, 1) : 0
            })
            .ToListAsync();

        return new
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            LostLeads = lostLeads,
            ActiveLeads = activeLeads,
            ConversionRate = conversionRate,
            BySource = sourceConversions
        };
    }

    public async Task<object> GetUserActivityReportAsync()
    {
        var activities = await _context.Activities.Include(a => a.AssignedUser).AsNoTracking().ToListAsync();

        var userActivity = activities.GroupBy(a => a.AssignedUser?.FullName ?? "Unassigned")
            .Select(g => new
            {
                UserName = g.Key,
                TotalActivities = g.Count(),
                Calls = g.Count(x => x.ActivityType == ActivityType.Call),
                Meetings = g.Count(x => x.ActivityType == ActivityType.Meeting),
                Emails = g.Count(x => x.ActivityType == ActivityType.Email),
                Tasks = g.Count(x => x.ActivityType == ActivityType.Task)
            })
            .ToList();

        return userActivity;
    }

    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string reportTitle)
    {
        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Title row
        sb.AppendLine($"# {reportTitle} - Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        // Header
        sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

        // Rows
        foreach (var item in data)
        {
            var line = properties.Select(p =>
            {
                var val = p.GetValue(item)?.ToString() ?? "";
                val = val.Replace("\"", "\"\"");
                return $"\"{val}\"";
            });
            sb.AppendLine(string.Join(",", line));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
