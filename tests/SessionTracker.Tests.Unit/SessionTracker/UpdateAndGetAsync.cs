namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class UpdateAndGetAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public UpdateAndGetAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.UpdateAndGetAsync(_fixture.Session, cts.Token));
        }


        [Fact]
        public async Task ReturnSuccessWithObtainedSession()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var session = _fixture.Session;
            var returnedSession = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                x.UpdateAndGetAsync(session, cts.Token)).ReturnsAsync(returnedSession);

            // Act 
            var result = await _fixture.Service.UpdateAndGetAsync(session, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Equal(returnedSession, result.Entity);

            _fixture.DataProviderMock.Verify(x => x.UpdateAndGetAsync(session, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ReturnErrorWhenDataProviderReturnsError()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var error = new InvalidOperationError();
            var session = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                x.UpdateAndGetAsync(session, cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.UpdateAndGetAsync(session, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x => x.UpdateAndGetAsync(session, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.UpdateAndGetAsync(It.IsAny<global::SessionTracker.Session>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }
    }
}