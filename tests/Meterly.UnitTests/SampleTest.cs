using Meterly.Domain.Common;

namespace Meterly.UnitTests;

/// <summary>
/// A dummy entity to test the BaseEntity behavior.
/// </summary>
public class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Proves the unit test project is configured correctly.
/// </summary>
public class SampleTest
{
    [Fact]
    public void BaseEntity_WhenCreated_HasIdAndTimestamps()
    {
        // Arrange & Act
        var entity = new TestEntity { Name = "Test" };

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
        
        // The created timestamp should be very recent (within the last minute)
        var timeSinceCreation = DateTime.UtcNow - entity.CreatedAt;
        Assert.True(timeSinceCreation.TotalMinutes < 1);
        
        // UpdatedAt should be essentially the same as CreatedAt initially
        // (they might differ by a few microseconds because they're separate DateTime.UtcNow calls)
        var difference = Math.Abs((entity.UpdatedAt - entity.CreatedAt).TotalSeconds);
        Assert.True(difference < 1, "CreatedAt and UpdatedAt should be within 1 second of each other");
    }
}
