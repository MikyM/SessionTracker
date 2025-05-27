namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class FinishAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public FinishAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token));
        }


        [Fact]
        public async Task ReturnSuccess()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var options = _fixture.SettingsMock.Object.Value.GetEvictionSessionEntryOptions<Session>();

            _fixture.DataProviderMock.Setup(x =>
                x.EvictAsync<Session>(_fixture.TestSessionKey, options, cts.Token)).ReturnsAsync(Result.FromSuccess);

            // Act 
            var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);

            _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, options, cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var options = _fixture.SettingsMock.Object.Value.GetEvictionSessionEntryOptions<Session>();

            _fixture.DataProviderMock.Setup(x =>
                x.EvictAsync<Session>(_fixture.TestSessionKey, options, cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, options, cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.EvictAsync<Session>(_fixture.TestSessionKey, It.IsAny<SessionEntryOptions>(),
                    It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }
    }
}