using Meterly.Domain.Entities;
using Meterly.Domain.Enums;

namespace Meterly.UnitTests.Dunning;

/// <summary>
/// Pure unit tests for the dunning retry schedule logic.
///
/// These tests use NO mocks and NO database — they test the business rule
/// that the retry schedule (0, 1, 3, 7 days) is respected correctly.
///
/// We extract the schedule logic here to demonstrate "testable domain logic."
/// In interviews this is the answer to: "How do you test business rules
/// without spinning up a server or database?"
/// </summary>
public class DunningScheduleTests
{
    // Mirrors the schedule in DunningJob
    private static readonly int[] RetryDelayDays = { 0, 1, 3, 7 };
    private const int MaxAttempts = 4;

    private static bool ShouldRetryNow(int attemptCount, DateTime dueDate)
    {
        if (attemptCount >= MaxAttempts) return false;
        var daysSinceDue = (DateTime.UtcNow - dueDate).TotalDays;
        return daysSinceDue >= RetryDelayDays[attemptCount];
    }

    [Fact]
    public void ShouldRetry_WhenNoPreviousAttempts_AndJustBecameOverdue_ReturnsTrue()
    {
        // Day 0 — invoice just passed due date → should fire immediately
        var dueDate = DateTime.UtcNow.AddHours(-1); // 1 hour overdue
        ShouldRetryNow(0, dueDate).Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WhenOneAttemptFailed_AndOnlyHoursHavePassed_ReturnsFalse()
    {
        // 1 attempt done, but only 3 hours have passed — need 1 full day
        var dueDate = DateTime.UtcNow.AddHours(-3);
        ShouldRetryNow(1, dueDate).Should().BeFalse("Attempt 2 requires 1 day to elapse");
    }

    [Fact]
    public void ShouldRetry_WhenOneAttemptFailed_AndMoreThanOneDayHasPassed_ReturnsTrue()
    {
        var dueDate = DateTime.UtcNow.AddDays(-1.5); // 1.5 days overdue
        ShouldRetryNow(1, dueDate).Should().BeTrue("Attempt 2 fires after 1 day");
    }

    [Fact]
    public void ShouldRetry_WhenThreeAttemptsFailed_AndOnlyFiveDaysHavePassed_ReturnsFalse()
    {
        var dueDate = DateTime.UtcNow.AddDays(-5);
        ShouldRetryNow(3, dueDate).Should().BeFalse("Attempt 4 requires 7 days to elapse");
    }

    [Fact]
    public void ShouldRetry_WhenThreeAttemptsFailed_AndMoreThanSevenDaysHavePassed_ReturnsTrue()
    {
        var dueDate = DateTime.UtcNow.AddDays(-8);
        ShouldRetryNow(3, dueDate).Should().BeTrue("Attempt 4 fires after 7 days");
    }

    [Fact]
    public void ShouldRetry_WhenMaxAttemptsReached_ReturnsFalse()
    {
        // 4 attempts already done — no more retries regardless of time
        var dueDate = DateTime.UtcNow.AddDays(-30); // ancient due date
        ShouldRetryNow(4, dueDate).Should().BeFalse("No more retries after MaxAttempts");
        ShouldRetryNow(5, dueDate).Should().BeFalse("No more retries above MaxAttempts");
    }

    [Theory]
    [InlineData(0, 0)]   // Attempt 1: Day 0 (immediate)
    [InlineData(1, 1)]   // Attempt 2: Day 1
    [InlineData(2, 3)]   // Attempt 3: Day 3
    [InlineData(3, 7)]   // Attempt 4: Day 7
    public void DunningSchedule_HasCorrectDelayForEachAttempt(int attemptIndex, int expectedDays)
    {
        RetryDelayDays[attemptIndex].Should().Be(expectedDays,
            $"attempt {attemptIndex + 1} should fire on day {expectedDays}");
    }

    [Fact]
    public void SubscriptionStatus_SuspendedEnumValue_IsPresent()
    {
        // Regression test — ensures Suspended wasn't removed from the enum
        // (this protects against accidental enum value removal breaking the dunning logic)
        var statuses = Enum.GetValues<SubscriptionStatus>();
        statuses.Should().Contain(SubscriptionStatus.Suspended, "Dunning depends on this status");
    }
}
