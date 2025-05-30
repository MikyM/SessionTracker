using RedLockNet;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis.Tests.Unit.RedisLockProvider;

public partial class RedisLockProvider
{
    [Collection("RedisLockProvider")]
    public class LockAsyncExpShould
    {
        private readonly RedisSessionTrackerDataProviderTestsFixture _fixture;

        public LockAsyncExpShould(RedisSessionTrackerDataProviderTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ThrowWhenCtCancelled()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            await cts.CancelAsync();

            // Act && Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _fixture.GetLockProvider(_fixture.RedLockFactoryMock.Object).AcquireAsync<Session>(_fixture.SessionKey, new TimeSpan(), cts.Token));
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }

        [Fact]
        public async Task ReturnErrorWhenProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var span = new TimeSpan(0, 0, 2, 0, 0);
            var errLock = new Mock<IRedLock>();
            errLock.SetupGet(x => x.IsAcquired).Returns(false);

            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, span)).ReturnsAsync(errLock.Object);

            // Act 
            var result = await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, span, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<SessionLockNotAcquiredError>(result.Error);

            factory.Verify(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, span), Times.Once);
        }

        [Fact]
        public async Task ReturnSuccessWhenProviderReturnsSuccess()
        {
            // Arrange
            _fixture.Reset();
            var span = new TimeSpan(0, 0, 2, 0, 0);
            var @lock = new Mock<IRedLock>();
            @lock.SetupGet(x => x.IsAcquired).Returns(true);
            
            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                    x.CreateLockAsync(_fixture.SessionLockKey, span))
                .ReturnsAsync(@lock.Object);

            // Act 
            var result = await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, span, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Entity);

            factory.Verify(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, span), Times.Once);
        }
    }
}