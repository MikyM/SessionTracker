namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class GetEvictedAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public GetEvictedAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token));
        }

        [Fact]
        public async Task ReturnSuccessWithObtainedSession()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var session = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(session);

            // Act 
            var result = await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Equal(session, result.Entity);

            _fixture.DataProviderMock.Verify(x => x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();

            _fixture.DataProviderMock.Setup(x =>
                x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x => x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.GetEvictedAsync<Session>(_fixture.TestSessionKey, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result =
                await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }
    }
}