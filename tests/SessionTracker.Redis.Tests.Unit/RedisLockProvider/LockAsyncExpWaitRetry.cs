using RedLockNet;
using SessionTracker.Abstractions;

namespace SessionTracker.Redis.Tests.Unit.RedisLockProvider;

public partial class RedisLockProvider
{
    [Collection("RedisLockProvider")]
    public class LockAsyncExpWaitRetryShould
    {
        private readonly RedisSessionTrackerDataProviderTestsFixture _fixture;

        public LockAsyncExpWaitRetryShould(RedisSessionTrackerDataProviderTestsFixture fixture)
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
                await _fixture.GetLockProvider(_fixture.RedLockFactoryMock.Object).AcquireAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(),
                    new TimeSpan(), cts.Token));
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                x.CreateLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                    CancellationToken.None)).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(),
                new TimeSpan(), CancellationToken.None);

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
            var exp = new TimeSpan(0, 0, 2, 0, 0);
            var wait = new TimeSpan(0, 0, 3, 0, 0);
            var retry = new TimeSpan(0, 0, 4, 0, 0);
            var err = new Mock<IRedLock>();
            err.SetupGet(x => x.IsAcquired).Returns(false);
            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, exp, wait, retry, CancellationToken.None)).ReturnsAsync(err.Object);

            // Act 
            var result =
                await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, exp, wait, retry,
                    CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);

            factory.Verify(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, exp, wait, retry, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task ReturnSuccessWhenProviderReturnsSuccess()
        {
            // Arrange
            _fixture.Reset();
            var exp = new TimeSpan(0, 0, 2, 0, 0);
            var wait = new TimeSpan(0, 0, 3, 0, 0);
            var retry = new TimeSpan(0, 0, 4, 0, 0);
            var @lock = new Mock<IRedLock>();
            @lock.SetupGet(x => x.IsAcquired).Returns(true);
            var factory = _fixture.RedLockFactoryMock;
            
            factory.Setup(x =>
                    x.CreateLockAsync(_fixture.SessionLockKey, exp, wait, retry, CancellationToken.None))
                .ReturnsAsync(@lock.Object);

            // Act 
            var result =
                await _fixture.GetLockProvider(factory.Object).AcquireAsync<Session>(_fixture.SessionKey, exp, wait, retry,
                    CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Entity);

            factory.Verify(x =>
                x.CreateLockAsync(_fixture.SessionLockKey, exp, wait, retry, CancellationToken.None), Times.Once);
        }
    }
}