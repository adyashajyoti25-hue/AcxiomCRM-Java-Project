using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;

namespace AcxiomCRM.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(string? dateFilter = null)
    {
        var now = DateTime.UtcNow;
        DateTime? startDate = dateFilter?.ToLower() switch
        {
            "today" => DateTime.Today,
            "week" => DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek),
            "month" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            _ => null
        };

        // Scoped queries
        var customerQ = _context.Customers.AsNoTracking().AsQueryable();
        var leadQ = _context.Leads.AsNoTracking().AsQueryable();
        var oppQ = _context.Opportunities.AsNoTracking().Include(o => o.Customer).AsQueryable();
        var followUpQ = _context.FollowUps.AsNoTracking().Include(f => f.Customer).Include(f => f.Lead).AsQueryable();

        if (_currentUser.IsSalesExecutive && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            customerQ = customerQ.Where(c => c.AssignedTo == _currentUser.UserId);
            leadQ = leadQ.Where(l => l.AssignedTo == _currentUser.UserId);
            oppQ = oppQ.Where(o => o.AssignedTo == _currentUser.UserId);
            followUpQ = followUpQ.Where(f => f.AssignedTo == _currentUser.UserId);
        }

        if (startDate.HasValue)
        {
            customerQ = customerQ.Where(c => c.CreatedDate >= startDate.Value);
            leadQ = leadQ.Where(l => l.CreatedDate >= startDate.Value);
            oppQ = oppQ.Where(o => o.CreatedDate >= startDate.Value);
        }

        var totalCustomers = await customerQ.CountAsync();
        var totalLeads = await leadQ.CountAsync();
        var openLeads = await leadQ.CountAsync(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost);

        var totalOpportunities = await oppQ.CountAsync();
        var openOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Open);
        var wonOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Won);
        var lostOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Lost);

        var openOppsList = await oppQ.Where(o => o.Status == OpportunityStatus.Open).ToListAsync();
        var totalPipelineValue = openOppsList.Sum(o => o.Amount);
        var weightedPipelineValue = openOppsList.Sum(o => o.WeightedValue);

        var today = DateTime.Today;
        var upcomingFollowUps = await followUpQ.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date >= today);
        var overdueFollowUps = await followUpQ.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today);

        // 1. Lead Status Chart
        var leadStatusCounts = await leadQ
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var allLeadStatuses = Enum.GetValues<LeadStatus>();
        var leadChart = new ChartDataDto
        {
            Labels = allLeadStatuses.Select(s => s.ToString()).ToList(),
            Data = allLeadStatuses.Select(s => (decimal)(leadStatusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0)).ToList(),
            Colors = new List<string> { "#3b82f6", "#06b6d4", "#10b981", "#64748b", "#8b5cf6", "#ef4444" }
        };

        // 2. Opportunity Stage Chart
        var oppStageRaw = await oppQ
            .Select(o => new { o.Stage, o.Amount })
            .ToListAsync();

        var oppStageTotals = oppStageRaw
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Total = g.Sum(x => x.Amount) })
            .ToList();

        var allStages = Enum.GetValues<OpportunityStage>();
        var oppChart = new ChartDataDto
        {
            Labels = allStages.Select(s => s.ToString()).ToList(),
            Data = allStages.Select(s => oppStageTotals.FirstOrDefault(x => x.Stage == s)?.Total ?? 0).ToList(),
            Colors = new List<string> { "#0284c7", "#f59e0b", "#8b5cf6", "#10b981", "#ef4444" }
        };

        // 3. Monthly Sales Chart (Last 6 months won values)
        var sixMonthsAgo = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var monthlySalesData = await oppQ
            .Where(o => o.Status == OpportunityStatus.Won && o.ExpectedCloseDate >= sixMonthsAgo)
            .ToListAsync();

        var monthlyChart = new ChartDataDto();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = today.AddMonths(-i);
            var label = targetMonth.ToString("MMM yyyy");
            var monthTotal = monthlySalesData
                .Where(o => o.ExpectedCloseDate.Year == targetMonth.Year && o.ExpectedCloseDate.Month == targetMonth.Month)
                .Sum(o => o.Amount);

            monthlyChart.Labels.Add(label);
            monthlyChart.Data.Add(monthTotal);
            monthlyChart.Colors.Add("#1e40af");
        }

        // Recent Follow-ups
        var recentFollowUps = await followUpQ
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
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

        // Top Opportunities
        var topOpportunitiesRaw = await oppQ
            .Where(o => o.Status == OpportunityStatus.Open)
            .Select(o => new
            {
                o.OpportunityId,
                o.OpportunityName,
                o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                o.Amount,
                o.Stage,
                o.Probability,
                o.ExpectedCloseDate,
                o.Status,
                o.AssignedTo
            })
            .ToListAsync();

        var topOpportunities = topOpportunitiesRaw
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo
            })
            .ToList();

        return new DashboardMetricsDto
        {
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpportunities,
            OpenOpportunities = openOpportunities,
            WonOpportunities = wonOpportunities,
            LostOpportunities = lostOpportunities,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            UpcomingFollowUps = upcomingFollowUps,
            OverdueFollowUps = overdueFollowUps,
            LeadStatusChart = leadChart,
            OpportunityStageChart = oppChart,
            MonthlySalesChart = monthlyChart,
            RecentFollowUps = recentFollowUps,
            TopOpportunities = topOpportunities
        };
    }
}
