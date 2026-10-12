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

public class OpportunityAndFollowUpServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateOpportunity_AmountZeroOrNegative_Rejects()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new OpportunityService(context, mockUser.Object, mockAudit.Object);

        var dto = new OpportunityCreateUpdateDto
        {
            OpportunityName = "Zero Amount Deal",
            CustomerId = 1,
            Amount = 0m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(10)
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be greater than 0", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(101)]
    public async Task CreateOpportunity_ProbabilityOutOfRange_Rejects(int probability)
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new OpportunityService(context, mockUser.Object, mockAudit.Object);

        var dto = new OpportunityCreateUpdateDto
        {
            OpportunityName = "Invalid Prob Deal",
            CustomerId = 1,
            Amount = 1000m,
            Probability = probability,
            ExpectedCloseDate = DateTime.Today.AddDays(10)
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Probability must be between 0 and 100", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOpportunity_ExpectedCloseDateInPast_Rejects()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new OpportunityService(context, mockUser.Object, mockAudit.Object);

        var dto = new OpportunityCreateUpdateDto
        {
            OpportunityName = "Past Date Deal",
            CustomerId = 1,
            Amount = 50000m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(-2),
            Status = OpportunityStatus.Open
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Expected Close Date cannot be in the past", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFollowUp_DateInPast_Rejects()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new FollowUpService(context, mockUser.Object, mockAudit.Object);

        var dto = new FollowUpCreateUpdateDto
        {
            Subject = "Yesterday Follow-up",
            FollowUpDate = DateTime.Today.AddDays(-1),
            Status = FollowUpStatus.Planned,
            AssignedTo = "user-1"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be earlier than today", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteFollowUp_SetsStatusToCompletedAndCreatesActivity()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var user = new ApplicationUser { Id = "user-1", UserName = "test@crm.com", FullName = "Test User" };
        context.Users.Add(user);

        var fu = new FollowUp
        {
            Subject = "Important Call",
            FollowUpDate = DateTime.Today.AddDays(1),
            Status = FollowUpStatus.Planned,
            AssignedTo = "user-1"
        };
        context.FollowUps.Add(fu);
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        mockUser.Setup(u => u.IsAdmin).Returns(true);
        var mockAudit = new Mock<IAuditService>();
        var service = new FollowUpService(context, mockUser.Object, mockAudit.Object);

        // Act
        var result = await service.CompleteAsync(fu.FollowUpId, "Client agreed on terms");

        // Assert
        Assert.True(result.Success, $"Failed with message: {result.Message}");
        var updated = await context.FollowUps.FindAsync(fu.FollowUpId);
        Assert.Equal(FollowUpStatus.Completed, updated!.Status);
        Assert.NotNull(updated.CompletedDate);

        // Verifies activity is also generated
        var activity = await context.Activities.FirstOrDefaultAsync(a => a.Subject.Contains("Important Call"));
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatus.Completed, activity.Status);
    }
}
