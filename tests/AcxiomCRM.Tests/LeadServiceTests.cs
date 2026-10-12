using Microsoft.EntityFrameworkCore;
using Moq;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Xunit;

namespace AcxiomCRM.Tests;

public class LeadServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateLead_ValidData_Succeeds()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new LeadService(context, mockUser.Object, mockAudit.Object);

        var dto = new LeadCreateUpdateDto
        {
            LeadName = "Fintech Prospect",
            Email = "lead@fintech.com",
            Phone = "9876543210",
            ExpectedValue = 50000m,
            Priority = LeadPriority.High,
            Source = LeadSource.Website,
            Status = LeadStatus.New
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Id > 0);
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact]
    public async Task ConvertLead_WhenLeadIsLost_RejectsConversion()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var lead = new Lead
        {
            LeadCode = "LEAD-2001",
            LeadName = "Lost Deal",
            Email = "lost@deal.com",
            Phone = "1234567890",
            Status = LeadStatus.Lost
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new LeadService(context, mockUser.Object, mockAudit.Object);

        var dto = new LeadConvertDto
        {
            LeadId = lead.LeadId,
            CreateOpportunity = false
        };

        // Act
        var result = await service.ConvertLeadAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be converted", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertLead_WhenOpportunityAmountIsZeroOrNegative_RejectsConversion()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var lead = new Lead
        {
            LeadCode = "LEAD-2001",
            LeadName = "Qualified Lead",
            Email = "qual@deal.com",
            Phone = "1234567890",
            Status = LeadStatus.Qualified
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new LeadService(context, mockUser.Object, mockAudit.Object);

        var dto = new LeadConvertDto
        {
            LeadId = lead.LeadId,
            CreateOpportunity = true,
            OpportunityAmount = -100m
        };

        // Act
        var result = await service.ConvertLeadAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be greater than 0", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
