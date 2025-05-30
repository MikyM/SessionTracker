namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class GetLockedAsyncAllShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public GetLockedAsyncAllShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.GetLockedAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                    new TimeSpan(), cts.Token));
        }

        [Fact]
        public async Task ReturnSuccessWithObtainedLock()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var expiry = new TimeSpan(1, 2, 3, 4, 5);
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedLock = new Mock<ISessionLock>();
            var returnedSession = _fixture.Session;

            _fixture.LockProviderMock.Setup(x =>
                    x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(Result<global::SessionTracker.Session>.FromSuccess(returnedSession));

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Same(result.Entity.Lock, returnedLock.Object);
            Assert.Same(result.Entity.Session, returnedSession);

            _fixture.LockProviderMock.Verify(
                x => x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderLockAsyncReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var expiry = new TimeSpan(1, 2, 3, 4, 5);
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedSession = _fixture.Session;

            _fixture.LockProviderMock.Setup(x =>
                    x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
                .ReturnsAsync(error);
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(Result<global::SessionTracker.Session>.FromSuccess(returnedSession));

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.LockProviderMock.Verify(
                x => x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderGetAsyncReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var expiry = new TimeSpan(1, 2, 3, 4, 5);
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedLock = new Mock<ISessionLock>();

            _fixture.LockProviderMock.Setup(x =>
                    x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(error);

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.LockProviderMock.Verify(
                x => x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
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
                x.AcquireAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));
            _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token)).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.GetLockedAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, new TimeSpan(),
                new TimeSpan(), new TimeSpan(), cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        
            @lock.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }
}