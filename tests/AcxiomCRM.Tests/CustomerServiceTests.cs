using Microsoft.EntityFrameworkCore;
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

public class CustomerServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateCustomer_ValidData_Succeeds()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        mockUser.Setup(u => u.IsAdmin).Returns(true);

        var mockAudit = new Mock<IAuditService>();
        var service = new CustomerService(context, mockUser.Object, mockAudit.Object);

        var dto = new CustomerCreateUpdateDto
        {
            CustomerName = "Acme Technologies",
            Email = "contact@acme.com",
            Phone = "9876543210",
            CompanyName = "Acme Inc.",
            Status = CustomerStatus.Active
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Id > 0);
        Assert.Equal(1, await context.Customers.CountAsync());
        mockAudit.Verify(a => a.LogAsync(AuditActions.Create, "Customer", It.IsAny<string>(), null, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_RejectsWithErrorMessage()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new CustomerService(context, mockUser.Object, mockAudit.Object);

        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1001",
            CustomerName = "First Customer",
            Email = "duplicate@acme.com",
            Phone = "9111111111"
        });
        await context.SaveChangesAsync();

        var dto = new CustomerCreateUpdateDto
        {
            CustomerName = "Second Customer",
            Email = "duplicate@acme.com",
            Phone = "9222222222"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("email address already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_RejectsWithErrorMessage()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        var mockAudit = new Mock<IAuditService>();
        var service = new CustomerService(context, mockUser.Object, mockAudit.Object);

        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-1001",
            CustomerName = "First Customer",
            Email = "first@acme.com",
            Phone = "9876543210"
        });
        await context.SaveChangesAsync();

        var dto = new CustomerCreateUpdateDto
        {
            CustomerName = "Second Customer",
            Email = "second@acme.com",
            Phone = "9876543210"
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("phone number already exists", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SalesExecutive_OnlyViewsAssignedCustomers()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        context.Customers.AddRange(
            new Customer { CustomerCode = "CUST-1001", CustomerName = "Exec Cust", Email = "e1@a.com", Phone = "111", AssignedTo = "exec-1" },
            new Customer { CustomerCode = "CUST-1002", CustomerName = "Other Cust", Email = "e2@a.com", Phone = "222", AssignedTo = "exec-2" }
        );
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("exec-1");
        mockUser.Setup(u => u.IsSalesExecutive).Returns(true);

        var mockAudit = new Mock<IAuditService>();
        var service = new CustomerService(context, mockUser.Object, mockAudit.Object);

        // Act
        var result = await service.GetCustomersAsync(null, null, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Exec Cust", result.Items.First().CustomerName);
    }
}
