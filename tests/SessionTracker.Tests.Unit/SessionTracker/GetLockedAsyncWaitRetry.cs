namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class GetLockedAsyncWaitRetryShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public GetLockedAsyncWaitRetryShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                    cts.Token));
        }


        [Fact]
        public async Task ReturnSuccessWithObtainedLock()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedLock = new Mock<ISessionLock>();
            var returnedSession = _fixture.Session;

            _fixture.LockProviderMock.Setup(x =>
                    x.AcquireAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                        cts.Token))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Same(result.Entity.Lock, returnedLock.Object);
            Assert.Same(result.Entity.Session, returnedSession);

            _fixture.LockProviderMock.Verify(
                x => x.AcquireAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                    cts.Token),
                Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
        }
        
        [Fact]
        public async Task ReturnExceptionErrorAndReleaseLockWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            var cts = _fixture.Cts;
            var @lock = new Mock<ISessionLock>();
        
            _fixture.LockProviderMock.Setup(x =>
                x.AcquireAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));
            _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token)).ThrowsAsync(ex);

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        
            @lock.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }
}