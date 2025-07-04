﻿namespace SessionTracker.Tests.Unit.SessionTracker;

public partial class SessionTracker
{
    [Collection("SessionTracker")]
    public class UpdateAsyncShould
    {
        private readonly SessionTrackerTestsFixture _fixture;

        public UpdateAsyncShould(SessionTrackerTestsFixture fixture)
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
                await _fixture.Service.UpdateAsync(_fixture.Session, cts.Token));
        }


        [Fact]
        public async Task ReturnSuccess()
        {
            // Arrange
            _fixture.Reset();
            var cts = _fixture.Cts;
            var session = _fixture.Session;

            _fixture.DataProviderMock.Setup(x =>
                x.UpdateAsync(session, cts.Token)).ReturnsAsync(Result.FromSuccess);

            // Act 
            var result = await _fixture.Service.UpdateAsync(session, cts.Token);

            // Assert
            Assert.True((bool)result.IsSuccess);
            Assert.Equal(2, session.Version);

            _fixture.DataProviderMock.Verify(x => x.UpdateAsync(session, cts.Token), Times.Once);
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
                x.UpdateAsync(session, cts.Token)).ReturnsAsync(error);

            // Act 
            var result = await _fixture.Service.UpdateAsync(session, cts.Token);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<InvalidOperationError>(result.Error);
            Assert.Same(error, result.Error);

            _fixture.DataProviderMock.Verify(x => x.UpdateAsync(session, cts.Token), Times.Once);
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DataProviderMock.Setup(x =>
                x.UpdateAsync(It.IsAny<global::SessionTracker.Session>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

            // Assert
            Assert.False((bool)result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }
    }
}