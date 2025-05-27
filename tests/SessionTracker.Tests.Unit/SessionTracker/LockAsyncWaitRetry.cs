/*namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class LockAsyncWaitRetryShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public LockAsyncWaitRetryShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
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

            _fixture.DataProviderMock.Setup(x =>
                    x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                        cts.Token))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));

            // Act 
            var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Same(result.Entity, returnedLock.Object);

            _fixture.DataProviderMock.Verify(
                x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                    cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);

            _fixture.DataProviderMock.Setup(x =>
                    x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                        cts.Token))
                .ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(
                x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                    cts.Token),
                Times.Once);
        }


        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                    It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result =
                await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan());

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }


        [Fact]
        public async Task ReturnErrorWhenDataProviderLockAsyncReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedSession = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                    x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                        cts.Token))
                .ReturnsAsync(error);
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(
                x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                    cts.Token),
                Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderGetAsyncReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
            var wait = new TimeSpan(2, 2, 3, 4, 5);
            var retry = new TimeSpan(3, 2, 3, 4, 5);
            var returnedLock = new Mock<ISessionLock>();

            _fixture.DataProviderMock.Setup(x =>
                    x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                        cts.Token))
                .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
            _fixture.DataProviderMock.Setup(x =>
                    x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
                .ReturnsAsync(error);

            // Act 
            var result =
                await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(
                x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry,
                    cts.Token), Times.Once);
            _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
        }
    }
}*/