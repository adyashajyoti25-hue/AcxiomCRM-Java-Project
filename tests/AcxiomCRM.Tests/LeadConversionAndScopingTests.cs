using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Constants;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Xunit;

namespace AcxiomCRM.Tests;

public class LeadConversionAndScopingTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SalesExecutive_OnlyViewsAssignedLeads()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        context.Leads.AddRange(
            new Lead { LeadCode = "LEAD-101", LeadName = "Exec 1 Lead", Email = "e1@test.com", Phone = "111", AssignedTo = "exec-1" },
            new Lead { LeadCode = "LEAD-102", LeadName = "Exec 2 Lead", Email = "e2@test.com", Phone = "222", AssignedTo = "exec-2" }
        );
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("exec-1");
        mockUser.Setup(u => u.IsSalesExecutive).Returns(true);

        var mockAudit = new Mock<IAuditService>();
        var service = new LeadService(context, mockUser.Object, mockAudit.Object);

        // Act
        var result = await service.GetLeadsAsync(null, null, null, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Exec 1 Lead", result.Items.First().LeadName);
    }

    [Fact]
    public async Task SalesExecutive_OnlyViewsAssignedOpportunities()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var customer = new Customer
        {
            CustomerCode = "CUST-001",
            CustomerName = "Enterprise Client",
            Email = "client@ent.com",
            Phone = "1234567890"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        context.Opportunities.AddRange(
            new Opportunity { OpportunityName = "Deal 1", CustomerId = customer.CustomerId, Amount = 10000, Probability = 50, ExpectedCloseDate = DateTime.Today.AddDays(5), AssignedTo = "exec-1" },
            new Opportunity { OpportunityName = "Deal 2", CustomerId = customer.CustomerId, Amount = 20000, Probability = 70, ExpectedCloseDate = DateTime.Today.AddDays(10), AssignedTo = "exec-2" }
        );
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("exec-1");
        mockUser.Setup(u => u.IsSalesExecutive).Returns(true);

        var mockAudit = new Mock<IAuditService>();
        var service = new OpportunityService(context, mockUser.Object, mockAudit.Object);

        // Act
        var result = await service.GetOpportunitiesAsync(null, null, null, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Deal 1", result.Items.First().OpportunityName);
    }

    [Fact]
    public async Task ConvertLead_QualifiedLead_CreatesCustomerAndOpportunityAtomically()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var lead = new Lead
        {
            LeadCode = "LEAD-500",
            LeadName = "Global Enterprises",
            Email = "contact@global.com",
            Phone = "9988776655",
            CompanyName = "Global Corp",
            Status = LeadStatus.Qualified,
            ExpectedValue = 75000m,
            AssignedTo = "exec-1"
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("exec-1");
        mockUser.Setup(u => u.IsAdmin).Returns(true);

        var mockAudit = new Mock<IAuditService>();
        var service = new LeadService(context, mockUser.Object, mockAudit.Object);

        var convertDto = new LeadConvertDto
        {
            LeadId = lead.LeadId,
            CreateOpportunity = true,
            OpportunityName = "Global Expansion Contract",
            OpportunityAmount = 75000m,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = "exec-1"
        };

        // Act
        var result = await service.ConvertLeadAsync(convertDto);

        // Assert
        Assert.True(result.Success, result.Message);

        // Verify Lead status updated
        var updatedLead = await context.Leads.FindAsync(lead.LeadId);
        Assert.Equal(LeadStatus.Converted, updatedLead!.Status);
        Assert.NotNull(updatedLead.ConvertedCustomerId);
        Assert.NotNull(updatedLead.ConvertedOpportunityId);

        // Verify Customer created
        var customer = await context.Customers.FindAsync(updatedLead.ConvertedCustomerId.Value);
        Assert.NotNull(customer);
        Assert.Equal("contact@global.com", customer.Email);
        Assert.Equal("9988776655", customer.Phone);
        Assert.Equal("Global Corp", customer.CustomerName);

        // Verify Opportunity created
        var opportunity = await context.Opportunities.FindAsync(updatedLead.ConvertedOpportunityId.Value);
        Assert.NotNull(opportunity);
        Assert.Equal("Global Expansion Contract", opportunity.OpportunityName);
        Assert.Equal(75000m, opportunity.Amount);
        Assert.Equal(OpportunityStage.Qualification, opportunity.Stage);

        // Verify Audit Log was recorded
        mockAudit.Verify(a => a.LogAsync(AuditActions.Convert, "Lead", lead.LeadCode, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
