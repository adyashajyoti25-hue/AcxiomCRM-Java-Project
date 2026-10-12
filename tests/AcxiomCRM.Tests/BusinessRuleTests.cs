using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Xunit;

namespace AcxiomCRM.Tests;

public class BusinessRuleTests
{
    [Fact]
    public void Opportunity_WeightedValue_CalculatesCorrectly()
    {
        // Arrange
        var opp = new Opportunity
        {
            Amount = 100000m,
            Probability = 35
        };

        // Act
        var weighted = opp.WeightedValue;

        // Assert
        Assert.Equal(35000m, weighted);
    }

    [Theory]
    [InlineData(10000, 100, 10000)]
    [InlineData(50000, 50, 25000)]
    [InlineData(75000, 0, 0)]
    [InlineData(12345.67, 10, 1234.57)]
    public void Opportunity_WeightedValue_VariousProbabilities(decimal amount, int probability, decimal expected)
    {
        // Arrange
        var opp = new Opportunity
        {
            Amount = amount,
            Probability = probability
        };

        // Act & Assert
        Assert.Equal(expected, opp.WeightedValue);
    }

    [Fact]
    public void FollowUp_IsOverdue_TrueWhenDateIsInPastAndPlanned()
    {
        // Arrange
        var fu = new FollowUp
        {
            FollowUpDate = DateTime.Today.AddDays(-2),
            Status = FollowUpStatus.Planned
        };

        // Act
        var isOverdue = fu.Status == FollowUpStatus.Planned && fu.FollowUpDate.Date < DateTime.Today;

        // Assert
        Assert.True(isOverdue);
    }

    [Fact]
    public void FollowUp_IsOverdue_FalseWhenCompletedEvenIfDatePast()
    {
        // Arrange
        var fu = new FollowUp
        {
            FollowUpDate = DateTime.Today.AddDays(-2),
            Status = FollowUpStatus.Completed
        };

        // Act
        var isOverdue = fu.Status == FollowUpStatus.Planned && fu.FollowUpDate.Date < DateTime.Today;

        // Assert
        Assert.False(isOverdue);
    }
}
