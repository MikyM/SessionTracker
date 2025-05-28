namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class RefreshAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public RefreshAsyncShould(SessionTrackerTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }

        [Fact]
        public async Task ReturnSuccess()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            _fixture.DataProviderMock.Setup(x =>
                x.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(Result.FromSuccess());

            // Act 
            var result = await _fixture.Service.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            _fixture.DataProviderMock.Verify(x =>
                x.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey,
                    cts.Token), Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();

            _fixture.DataProviderMock.Setup(x =>
                x.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x =>
                x.RefreshAsync<global::SessionTracker.Session>(_fixture.TestSessionKey,
                    cts.Token), Times.Once);
        }
    }
}