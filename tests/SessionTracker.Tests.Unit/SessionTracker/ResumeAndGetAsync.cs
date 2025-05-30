namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class ResumeAndGetAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public ResumeAndGetAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.ResumeAndGetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token));
        }


        [Fact]
        public async Task ReturnSuccessWithObtainedSession()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var session = _fixture.Session;
            var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<global::SessionTracker.Session>();
            var returnedSession = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                    x.RestoreAndGetAsync<global::SessionTracker.Session>(session.Key, It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow), cts.Token))
                .ReturnsAsync(returnedSession);

            // Act 
            var result = await _fixture.Service.ResumeAndGetAsync<global::SessionTracker.Session>(session.Key, cts.Token);

            // Assert
            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Equal(returnedSession, result.Entity);

            _fixture.DataProviderMock.Verify(x => x.RestoreAndGetAsync<global::SessionTracker.Session>(session.Key,
                    It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow), cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<global::SessionTracker.Session>();

            _fixture.DataProviderMock.Setup(x =>
                    x.RestoreAndGetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow), cts.Token))
                .ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.ResumeAndGetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x => x.RestoreAndGetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey,
                    It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow), cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.RestoreAndGetAsync<global::SessionTracker.Session>(It.IsAny<string>(), It.IsAny<SessionEntryOptions>(),
                    It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result =
                await _fixture.Service.ResumeAndGetAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }
    }
}