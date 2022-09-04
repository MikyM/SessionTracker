using System;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace SessionTracker.Unit.Tests;

[UsedImplicitly]
public class SessionTests
{
    [Fact]
    public void Session_Should_Have_Correct_Initial_State()
    {
        var now = DateTimeOffset.UtcNow;
        var dt = new Mock<DateTimeProvider>();
        dt.SetupGet(x => x.OffsetUtcNow).Returns(now);
        DateTimeProvider.SetProvider(dt.Object);
        
        // Arrange
        var key = "test";
        
        // Act
        var session = new Session("test");
        
        // Assert
        
        Assert.Equal(key, session.Key);
        Assert.Equal(1, session.Version);
        Assert.Equal(now, session.StartedAt);
        
        DateTimeProvider.ResetToDefault();
    }
}
