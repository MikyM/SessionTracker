using SessionTracker.Abstractions;

namespace SessionTracker.Redis.Unit.Tests.RedisDataProvider;

public partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
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
                await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(),
                    new TimeSpan(), cts.Token));
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.LockProviderMock.Setup(x =>
                x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                    CancellationToken.None)).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(),
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
            var err = new InvalidOperationError();
            _fixture.LockProviderMock.Setup(x =>
                x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None)).ReturnsAsync(err);

            // Act 
            var result =
                await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, exp, wait, retry,
                    CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Equal(err, result.Error);

            _fixture.LockProviderMock.Verify(x =>
                x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task ReturnSuccessWhenProviderReturnsSuccess()
        {
            // Arrange
            _fixture.Reset();
            var exp = new TimeSpan(0, 0, 2, 0, 0);
            var wait = new TimeSpan(0, 0, 3, 0, 0);
            var retry = new TimeSpan(0, 0, 4, 0, 0);
            var @lock = new Mock<ISessionLock>();
            _fixture.LockProviderMock.Setup(x =>
                    x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));

            // Act 
            var result =
                await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, exp, wait, retry,
                    CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Same(@lock.Object, result.Entity);

            _fixture.LockProviderMock.Verify(x =>
                x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None), Times.Once);
        }
    }
}