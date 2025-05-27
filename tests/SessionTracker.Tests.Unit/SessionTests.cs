using JetBrains.Annotations;

namespace SessionTracker.Tests.Unit;

[UsedImplicitly]
[CollectionDefinition("Session")]
public class SessionTests
{
    [Collection("Session")]
    public class StateShould
    {
        [Fact]
        public void BeCorrectUponCreation()
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
}
