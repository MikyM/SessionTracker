namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class StartAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public StartAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.StartAsync(_fixture.Session, cts.Token));
        }

        [Fact]
        public async Task PassSessionAndCt()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var session = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                x.AddAsync(session, It.IsAny<SessionEntryOptions>(), cts.Token)).ReturnsAsync(Result.FromSuccess);

            // Act 
            var result = await _fixture.Service.StartAsync(session, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);

            _fixture.DataProviderMock.Verify(x => x.AddAsync(session, It.IsAny<SessionEntryOptions>(), cts.Token),
                Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                    x.AddAsync(It.IsAny<Session>(), It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.StartAsync(_fixture.Session, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }

        [Fact]
        public async Task PassProperEntryOptions()
        {
            // Arrange
            _fixture.Reset();
            var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<Session>();
            _fixture.DataProviderMock.Setup(x =>
                x.AddAsync(It.IsAny<Session>(),
                    It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow),
                    It.IsAny<CancellationToken>())).ReturnsAsync(Result.FromSuccess());

            // Act 
            var result = await _fixture.Service.StartAsync(_fixture.Session, CancellationToken.None);

            // Assert
            Assert.True((bool)result.IsSuccess);
            _fixture.DataProviderMock.Verify(x =>
                x.AddAsync(It.IsAny<Session>(),
                    It.Is<SessionEntryOptions>(y =>
                        y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                        y.SlidingExpiration == expected.SlidingExpiration &&
                        y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReturnSuccess()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            _fixture.DataProviderMock.Setup(x =>
                x.AddAsync(It.IsAny<Session>(),
                    It.IsAny<SessionEntryOptions>(), cts.Token)).ReturnsAsync(Result.FromSuccess());

            // Act 
            var result = await _fixture.Service.StartAsync(_fixture.Session, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            _fixture.DataProviderMock.Verify(x =>
                x.AddAsync(It.IsAny<Session>(),
                    It.IsAny<SessionEntryOptions>(),
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
                x.AddAsync(It.IsAny<Session>(),
                    It.IsAny<SessionEntryOptions>(), cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.StartAsync(_fixture.Session, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x =>
                x.AddAsync(It.IsAny<Session>(),
                    It.IsAny<SessionEntryOptions>(),
                    cts.Token), Times.Once);
        }
    }
}