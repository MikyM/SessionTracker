using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Remora.Results;
using SessionTracker.Abstractions;
using StackExchange.Redis;
using Xunit;

namespace SessionTracker.Redis.Unit.Tests;

[UsedImplicitly]
public class RedisSessionTrackerDataProviderTestsFixture
{
    public readonly Mock<ISessionLockProvider> LockProviderMock = new();
    public readonly Mock<IOptions<RedisSessionSettings>> SettingsMock = new();
    public readonly Mock<IOptionsMonitor<JsonSerializerOptions>> JsonSettingsMock = new();
    public readonly Mock<IConnectionMultiplexer> MultiplexerMock = new();
    public readonly Mock<IDatabase> DatabaseMock = new();
    
    public Session Session { get; }
    public string TestKeyEvicted => "sessions:evicted:session:test";
    public string TestKey => "sessions:session:test";

    public CancellationTokenSource Cts => new ();
    public string SessionKey => "test";
    public string Serialized { get; }

    public RedisSessionTrackerDataProviderTestsFixture()
    {
        Session = new Session(SessionKey);
        Serialized = JsonSerializer.Serialize(Session);
        var opt = new RedisSessionSettings();
        SettingsMock.Setup(x => x.Value).Returns(opt);
        var jopt = new JsonSerializerOptions();
        JsonSettingsMock.Setup(x => x.Get(RedisSessionSettings.JsonSerializerName)).Returns(jopt);

        MultiplexerMock.Setup(x => x.GetDatabase(-1, null)).Returns(DatabaseMock.Object);

        Service = new(SettingsMock.Object, MultiplexerMock.Object, NullLogger<RedisSessionTrackerDataProvider>.Instance,
            JsonSettingsMock.Object, LockProviderMock.Object);
    }

    public RedisSessionTrackerDataProvider Service { get; }

    public void Reset()
    {
        LockProviderMock.Reset();
        DatabaseMock.Reset();
    }
}

[Trait("data-provider", "redis")]
public class RedisSessionTrackerDataProviderTests : IClassFixture<RedisSessionTrackerDataProviderTestsFixture>
{
    private readonly RedisSessionTrackerDataProviderTestsFixture _fixture;

    public RedisSessionTrackerDataProviderTests(RedisSessionTrackerDataProviderTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetEvictedAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task GetEvictedAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.GetEvictedScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task GetEvictedAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0].Equals(new RedisKey(_fixture.TestKeyEvicted))), It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()));
    }

    [Fact]
    public async Task GetEvictedAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_An_Error_When_Deserialization_Fails()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(), It.Is<RedisValue[]?>(y => y == null || y.Length == 0),
            It.IsAny<CommandFlags>()));
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(_fixture.Serialized, ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.GetEvictedAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(_fixture.SessionKey, result.Entity.Key);
    }
    
    [Fact]
    public async Task GetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task GetAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.GetAndRefreshScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task GetAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y![0].Equals(new RedisKey(_fixture.TestKey))), It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()));
    }

    [Fact]
    public async Task GetAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_An_Error_When_Deserialization_Fails()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task GetAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(), It.Is<RedisValue[]?>(y => y != null && y.Length == 1 && y[0] == LuaScripts.ReturnDataArg),
            It.IsAny<CommandFlags>()));
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(_fixture.Serialized, ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(_fixture.SessionKey, result.Entity.Key);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.GetAndRefreshScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 1 && y[0] == LuaScripts.ReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), cts.Token));
    }
    
    [Fact]
    public async Task AddAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task AddAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.SetNotExistsScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task AddAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0].Equals(new RedisKey(_fixture.TestKey))), It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()));
    }

    [Fact]
    public async Task AddAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }
    
    [Fact]
    public async Task AddAsync_Should_Return_An_Error_When_Deserialization_Fails_And_Extracted_Value_IsNot_1()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task AddAsync_Should_Return_SessionInProgressError_When_Extracted_Value_IsNot_1()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(_fixture.Serialized, ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.AddAsync(_fixture.Session, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<SessionInProgressError>(result.Error);
    }
    
    [Fact]
    public async Task AddAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        _ = await _fixture.Service.AddAsync(_fixture.Session, opt, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(), It.Is<RedisValue[]?>(y =>
                y != null && y.Length == 4 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                y[2] == sld.TotalSeconds &&
                y[3] == JsonSerializer.Serialize(_fixture.Session, new JsonSerializerOptions())),
            CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(LuaScripts.SuccessfulScriptNoDataReturnValue), ResultType.BulkString));
        
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        var result = await _fixture.Service.AddAsync(_fixture.Session, opt, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.SetNotExistsScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey), It.Is<RedisValue[]?>(y =>
                y != null && y.Length == 4 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                y[2] == sld.TotalSeconds &&
                y[3] == JsonSerializer.Serialize(_fixture.Session, new JsonSerializerOptions())),
            CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, cts.Token)); }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task RefreshAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.GetAndRefreshScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task RefreshAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task RefreshAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 1 && y[0] == LuaScripts.DontReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(LuaScripts.SuccessfulScriptNoDataReturnValue), ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.SessionKey, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.GetAndRefreshScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 1 && y[0] == LuaScripts.DontReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAsync(_fixture.Session, cts.Token)); }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task UpdateAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 2 && y[0] == _fixture.Serialized && y[1] == LuaScripts.DontReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(LuaScripts.SuccessfulScriptNoDataReturnValue), ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 2 && y[0] == _fixture.Serialized && y[1] == LuaScripts.DontReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAndGetAsync(_fixture.Session, cts.Token)); }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task UpdateAndGetAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task UpdateAndGetAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_An_Error_When_Deserialization_Fails()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task UpdateAndGetAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 2 && y[0] == _fixture.Serialized && y[1] == LuaScripts.ReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(_fixture.Session, result.Entity);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.UpdateExistsAndRefreshConditionalReturnLastScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 2 && y[0] == _fixture.Serialized && y[1] == LuaScripts.ReturnDataArg),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_Exp_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), cts.Token)); }
    
    [Fact]
    public async Task LockAsync_Exp_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), CancellationToken.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task LockAsync_Exp_Should_Return_Error_When_Provider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var span = new TimeSpan(0, 0, 2, 0, 0);
        var err = new InvalidOperationError();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(_fixture.TestKey, span, CancellationToken.None)).ReturnsAsync(err);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, span, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(err, result.Error);

        _fixture.LockProviderMock.Verify(x =>
            x.AcquireAsync(_fixture.TestKey, span, CancellationToken.None), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_Exp_Should_Return_Success_When_Provider_Returns_Success()
    {
        // Arrange
        _fixture.Reset();
        var span = new TimeSpan(0, 0, 2, 0, 0);
        var @lock = new Mock<ISessionLock>();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(_fixture.TestKey, span, CancellationToken.None)).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, span, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(@lock.Object, result.Entity);

        _fixture.LockProviderMock.Verify(x =>
            x.AcquireAsync(_fixture.TestKey, span, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LockAsync_Exp_Wait_Retry_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();

        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(),
                new TimeSpan(), cts.Token));
    }

    [Fact]
    public async Task LockAsync_Exp_Wait_Retry_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), CancellationToken.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, new TimeSpan(), new TimeSpan(), new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task LockAsync_Exp_Wait_Retry_Should_Return_Error_When_Provider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var wait = new TimeSpan(0, 0, 3, 0, 0);
        var retry = new TimeSpan(0, 0, 4, 0, 0);
        var err = new InvalidOperationError();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None)).ReturnsAsync(err);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, exp, wait, retry, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(err, result.Error);

        _fixture.LockProviderMock.Verify(x =>
            x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_Exp_Wait_Retry_Should_Return_Success_When_Provider_Returns_Success()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var wait = new TimeSpan(0, 0, 3, 0, 0);
        var retry = new TimeSpan(0, 0, 4, 0, 0);
        var @lock = new Mock<ISessionLock>();
        _fixture.LockProviderMock.Setup(x =>
            x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None)).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.SessionKey, exp, wait, retry, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(@lock.Object, result.Entity);

        _fixture.LockProviderMock.Verify(x =>
            x.AcquireAsync(_fixture.TestKey, exp, wait, retry, CancellationToken.None), Times.Once);
    }
    
    [Fact]
    public async Task EvictAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), cts.Token)); }
    
    [Fact]
    public async Task EvictAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task EvictAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.RemoveMoveToEvictedScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task EvictAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task EvictAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task EvictAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task EvictAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var sec = RedisSessionTrackerDataProvider.GetEvictedExpirationInSeconds(exp);

        // Act 
        _ = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, exp, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RemoveMoveToEvictedScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 3 && y[0] == sec && y[1] == LuaScripts.DontReturnDataArg && y[2] == _fixture.TestKeyEvicted),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task EvictAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var sec = RedisSessionTrackerDataProvider.GetEvictedExpirationInSeconds(exp);
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(LuaScripts.SuccessfulScriptNoDataReturnValue), ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.EvictAsync<Session>(_fixture.SessionKey, exp, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RemoveMoveToEvictedScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 3 && y[0] == sec && y[1] == LuaScripts.DontReturnDataArg && y[2] == _fixture.TestKeyEvicted),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), cts.Token)); }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task EvictAndGetAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.RemoveMoveToEvictedScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task EvictAndGetAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Return_An_Error_When_Deserialization_Fails()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, new TimeSpan(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task EvictAndGetAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var sec = RedisSessionTrackerDataProvider.GetEvictedExpirationInSeconds(exp);

        // Act 
        _ = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, exp, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 3 && y[0] == sec && y[1] == LuaScripts.ReturnDataArg && y[2] == _fixture.TestKeyEvicted),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task EvictAndGetAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var exp = new TimeSpan(0, 0, 2, 0, 0);
        var sec = RedisSessionTrackerDataProvider.GetEvictedExpirationInSeconds(exp);
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.EvictAndGetAsync<Session>(_fixture.SessionKey, exp, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(_fixture.Session, result.Entity);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RemoveMoveToEvictedScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKey),
            It.Is<RedisValue[]?>(y => y != null && y.Length == 3 && y[0] == sec && y[1] == LuaScripts.ReturnDataArg && y[2] == _fixture.TestKeyEvicted),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task RestoreAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), cts.Token)); }
    
    [Fact]
    public async Task RestoreAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task RestoreAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.RestoreMoveToRegularScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task RestoreAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task RestoreAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task RestoreAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task RestoreAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));
        
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        _ = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.IsAny<RedisKey[]?>(), It.Is<RedisValue[]?>(y =>
                y != null && y.Length == 5 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                y[2] == sld.TotalSeconds &&
                y[3] == LuaScripts.DontReturnDataArg &&
                y[4] == _fixture.TestKey),
            CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task RestoreAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(LuaScripts.SuccessfulScriptNoDataReturnValue), ResultType.BulkString));
        
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        var result = await _fixture.Service.RestoreAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RestoreMoveToRegularScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted), It.Is<RedisValue[]?>(y =>
                y != null && y.Length == 5 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                y[2] == sld.TotalSeconds &&
                y[3] == LuaScripts.DontReturnDataArg &&
                y[4] == _fixture.TestKey),
            CommandFlags.None), Times.Once);
    }
    
    [Fact]
    public async Task RestoreAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), cts.Token)); }
    
    [Fact]
    public async Task RestoreAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task RestoreAndGetAsync_Should_Use_Proper_Lua_Script()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.Is<string>(y => y == LuaScripts.RestoreMoveToRegularScript),
            It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(), CommandFlags.None));
    }

    [Fact]
    public async Task RestoreAndGetAsync_Should_Pass_Proper_Key_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();

        // Act 
        _ = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(It.IsAny<string>(),
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted),
            It.IsAny<RedisValue[]?>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task RestoreAndGetAsync_Should_Return_NotFoundError_When_RedisResult_Is_Null()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(RedisValue.Null));

        // Act 
        var result = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<NotFoundError>(result.Error);
    }
    
    [Fact]
    public async Task RestoreAndGetAsync_Should_Return_An_Error_When_Deserialization_Fails()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create("zzz", ResultType.BulkString));

        // Act 
        var result = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
    }
    
    [Fact]
    public async Task RestoreAndGetAsync_Should_Return_UnexpectedRedisResultError_When_TryExtractString_Returns_False()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(1));

        // Act 
        var result = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, new SessionEntryOptions(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<UnexpectedRedisResultError>(result.Error);
    }

    [Fact]
    public async Task RestoreAndGetAsync_Should_Pass_Proper_RedisValues_To_ScriptEvaluateAsync()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));
        
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        _ = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

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
    public async Task RestoreAndGetAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        _fixture.DatabaseMock.Setup(x =>
            x.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]?>(), It.IsAny<RedisValue[]?>(),
                CommandFlags.None)).ReturnsAsync(RedisResult.Create(new RedisValue(_fixture.Serialized), ResultType.BulkString));
        
        var opt = new SessionEntryOptions();
        var sld = new TimeSpan(0, 0, 2, 2, 0);
        var abs = RedisSessionTrackerDataProvider.GetAbsoluteExpiration(DateTimeOffset.UtcNow, opt);
        opt.SetSlidingExpiration(sld);
        var absUnix = abs?.ToUnixTimeSeconds() ?? LuaScripts.NotPresent;

        // Act 
        var result = await _fixture.Service.RestoreAndGetAsync<Session>(_fixture.SessionKey, opt, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(_fixture.Session, result.Entity);

        _fixture.DatabaseMock.Verify(x => x.ScriptEvaluateAsync(LuaScripts.RestoreMoveToRegularScript,
            It.Is<RedisKey[]?>(y => y != null && y.Length == 1 && y[0] == _fixture.TestKeyEvicted), It.Is<RedisValue[]?>(y =>
                y != null && y.Length == 5 && y[0] == absUnix && y[1] == sld.TotalSeconds &&
                y[2] == sld.TotalSeconds &&
                y[3] == LuaScripts.ReturnDataArg &&
                y[4] == _fixture.TestKey),
            CommandFlags.None), Times.Once);
    }
}
