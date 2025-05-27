namespace SessionTracker.Redis.Tests.Unit.RedisDataProvider;

public partial class RedisDataProvider
{
    [Collection("RedisDataProvider")]
    public class RestoreAndGetAsyncShould
    {
        private readonly RedisSessionTrackerDataProviderTestsFixture _fixture;

        public RestoreAndGetAsyncShould(RedisSessionTrackerDataProviderTestsFixture fixture)
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
                await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(),
                    cts.Token));
        }

        [Fact]
        public async Task ReturnExceptionErrorWhenExIsCaught()
        {
            // Arrange
            _fixture.Reset();
            var ex = new InvalidOperationException();
            _fixture.DatabaseMock.Setup(x =>
                x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                    CommandFlags.None)).ThrowsAsync(ex);

            // Act 
            var result = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey,
                new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
            Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        }

        [Fact]
        public async Task UseProperLuaScript()
        {
            // Arrange
            _fixture.Reset();

            // Act 
            _ = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(
                It.Is<string>(y => y == LuaScripts.RestoreMoveToRegularScript),
                It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
        }

        [Fact]
        public async Task PassProperKeyToScriptEvaluateAsync()
        {
            // Arrange
            _fixture.Reset();

            // Act 
            _ = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
                It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted),
                It.IsAny<RedisValue[]?>(),
                It.IsAny<CommandFlags>()), Times.Once);
        }

        [Fact]
        public async Task ReturnNotFoundErrorWhenRedisResultIsNull()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                    CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

            // Act 
            var result = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey,
                new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<NotFoundError>(result.Error);
        }

        [Fact]
        public async Task ReturnAnErrorWhenDeserializationFails()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                    CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

            // Act 
            var result = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey,
                new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<ExceptionError>(result.Error);
        }

        [Fact]
        public async Task ReturnUnexpectedRedisResultErrorWhenTryExtractStringReturnsFalse()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                    CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

            // Act 
            var result = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey,
                new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<UnexpectedRedisResultError>(result.Error);
        }

        [Fact]
        public async Task ReturnSessionAlreadyRestoredErrorWhenRedisResultIs0()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                    CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue("0")));

            // Act 
            var result = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey,
                new SessionEntryOptions(),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.IsType<SessionAlreadyRestoredError>(result.Error);
        }

        [Fact]
        public async Task PassProperRedisValuesToScriptEvaluateAsync()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                    x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                        CommandFlags.None))
                .ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));

            var opt = new SessionEntryOptions();
            var sld = new TimeSpan(0, 0, 2, 2, 0);
            var abs = Redis.RedisSessionDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
            opt.SetSlidingExpiration(sld);
            var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

            // Act 
            _ = await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

            // Assert

            _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
                It.IsAny<RedisKey[]?>(), It.Is<RedisValue[]?>(y =>
                    y != null && y.Length == 5 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                    y[2] == sld.TotalSeconds &&
                    y[3] == LuaScripts.ReturnDataArg &&
                    y[4] == _fixture.TestKey),
                CommandFlags.None), Times.Once);
        }

        [Fact]
        public async Task ReturnSuccess()
        {
            // Arrange
            _fixture.Reset();
            _fixture.DatabaseMock.Setup(x =>
                    x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                        CommandFlags.None))
                .ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));

            var opt = new SessionEntryOptions();
            var sld = new TimeSpan(0, 0, 2, 2, 0);
            var abs = Redis.RedisSessionDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
            opt.SetSlidingExpiration(sld);
            var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

            // Act 
            var result =
                await _fixture.DataProvider.RestoreAndGetAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Entity);
            Assert.Equal(_fixture.Session, result.Entity);

            _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RestoreMoveToRegularScript,
                It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted),
                It.Is<RedisValue[]?>(y =>
                    y != null && y.Length == 5 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                    y[2] == sld.TotalSeconds &&
                    y[3] == LuaScripts.ReturnDataArg &&
                    y[4] == _fixture.TestKey),
                CommandFlags.None), Times.Once);
        }
    }
}
