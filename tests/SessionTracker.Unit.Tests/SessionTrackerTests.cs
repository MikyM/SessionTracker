using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Remora.Results;
using SessionTracker.Abstractions;
using Xunit;

namespace SessionTracker.Unit.Tests;

public class SessionTrackerTestsFixture
{
    public readonly Mock<ISessionTrackerDataProvider> DataProviderMock = new();
    public readonly Mock<IOptions<SessionSettings>> SettingsMock = new();
    public Session Session => new(TestSessionKey);
    public string TestSessionKey => "test";
    public CancellationTokenSource Cts => new ();

    public SessionTrackerTestsFixture()
    {
        var opt = new SessionSettings();
        SettingsMock.Setup(x => x.Value).Returns(opt);
    }

    public SessionTracker Service
        => new (DataProviderMock.Object, SettingsMock.Object);

    public void Reset()
        => DataProviderMock.Reset();
}

public class SessionTrackerTests : IClassFixture<SessionTrackerTestsFixture>
{
    private readonly SessionTrackerTestsFixture _fixture;

    public SessionTrackerTests(SessionTrackerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.StartAsync(_fixture.Session, cts.Token));
    }
    
    [Fact]
    public async Task StartAsync_Should_Pass_Session_And_CT()
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
        Assert.True(result.IsSuccess);
        
        _fixture.DataProviderMock.Verify(x => x.AddAsync(session,It.IsAny<SessionEntryOptions>(), cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task StartAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.AddAsync(It.IsAny<Session>(), It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.StartAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task StartAsync_Should_Pass_Proper_EntryOptions()
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
        Assert.True(result.IsSuccess);
        _fixture.DataProviderMock.Verify(x =>
            x.AddAsync(It.IsAny<Session>(),
                It.Is<SessionEntryOptions>(y =>
                    y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                    y.SlidingExpiration == expected.SlidingExpiration &&
                    y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow),
                It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task StartAsync_Should_Return_Success()
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
        Assert.True(result.IsSuccess);
        _fixture.DataProviderMock.Verify(x =>
            x.AddAsync(It.IsAny<Session>(),
                It.IsAny<SessionEntryOptions>(),
                cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task StartAsync_Should_Return_Error_When_DataProvider_Returns_Error()
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
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x =>
            x.AddAsync(It.IsAny<Session>(),
                It.IsAny<SessionEntryOptions>(),
                cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_Success_With_Obtained_Session()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey,  cts.Token)).ReturnsAsync(session);

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(session, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_Success_With_Obtained_Session()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.GetEvictedAsync<Session>(_fixture.TestSessionKey,  cts.Token)).ReturnsAsync(session);

        // Act 
        var result = await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(session, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        
        _fixture.DataProviderMock.Setup(x =>
            x.GetEvictedAsync<Session>(_fixture.TestSessionKey,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.GetEvictedAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetEvictedAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.GetEvictedAsync<Session>(_fixture.TestSessionKey, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }

    [Fact]
    public async Task RefreshAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.RefreshAsync<Session>(_fixture.TestSessionKey, It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        _fixture.DataProviderMock.Setup(x =>
            x.RefreshAsync<Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(Result.FromSuccess());

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        _fixture.DataProviderMock.Verify(x =>
            x.RefreshAsync<Session>(_fixture.TestSessionKey,
                cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task RefreshAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        
        _fixture.DataProviderMock.Setup(x =>
            x.RefreshAsync<Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.RefreshAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x =>
            x.RefreshAsync<Session>(_fixture.TestSessionKey,
                cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAsync(_fixture.Session, cts.Token));
    }
    
    
    [Fact]
    public async Task UpdateAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAsync(session,  cts.Token)).ReturnsAsync(Result.FromSuccess);

        // Act 
        var result = await _fixture.Service.UpdateAsync(session, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, session.Version);

        _fixture.DataProviderMock.Verify(x => x.UpdateAsync(session, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var session = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAsync(session,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.UpdateAsync(session, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.UpdateAsync(session, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.UpdateAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAndGetAsync(_fixture.Session, cts.Token));
    }
    
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_Success_With_Obtained_Session()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        var returnedSession = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAndGetAsync(session,  cts.Token)).ReturnsAsync(returnedSession);

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(session, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(returnedSession, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.UpdateAndGetAsync(session, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var session = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAndGetAsync(session,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(session, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.UpdateAndGetAsync(session, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.UpdateAndGetAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.UpdateAndGetAsync(_fixture.Session, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task FinishAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task FinishAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var ts = _fixture.SettingsMock.Object.Value.GetEvictionAbsoluteExpirationOrDefault<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.EvictAsync<Session>(_fixture.TestSessionKey, ts!.Value , cts.Token)).ReturnsAsync(Result.FromSuccess);

        // Act 
        var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, ts!.Value ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var ts = _fixture.SettingsMock.Object.Value.GetEvictionAbsoluteExpirationOrDefault<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.EvictAsync<Session>(_fixture.TestSessionKey, ts!.Value , cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, ts!.Value ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.EvictAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task FinishAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task FinishAndGetAsync_Should_Return_Success_With_Obtained_Session()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        var ts = _fixture.SettingsMock.Object.Value.GetEvictionAbsoluteExpirationOrDefault<Session>();
        var returnedSession = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.RemoveAndGetAsync<Session>(session.Key, ts!.Value,  cts.Token)).ReturnsAsync(returnedSession);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(session.Key, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(returnedSession, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.RemoveAndGetAsync<Session>(session.Key, ts!.Value, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAndGetAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var ts = _fixture.SettingsMock.Object.Value.GetEvictionAbsoluteExpirationOrDefault<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.RemoveAndGetAsync<Session>(_fixture.TestSessionKey, ts!.Value,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.RemoveAndGetAsync<Session>(_fixture.TestSessionKey, ts!.Value, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.RemoveAndGetAsync<Session>(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task ResumeAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.ResumeAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task ResumeAsync_Should_Return_Success()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
                y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                y.SlidingExpiration == expected.SlidingExpiration &&
                y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) , cts.Token)).ReturnsAsync(Result.FromSuccess);

        // Act 
        var result = await _fixture.Service.ResumeAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);

        _fixture.DataProviderMock.Verify(x => x.RestoreAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
            y.AbsoluteExpiration == expected.AbsoluteExpiration &&
            y.SlidingExpiration == expected.SlidingExpiration &&
            y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task ResumeAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
                y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                y.SlidingExpiration == expected.SlidingExpiration &&
                y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) , cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.ResumeAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.RestoreAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
            y.AbsoluteExpiration == expected.AbsoluteExpiration &&
            y.SlidingExpiration == expected.SlidingExpiration &&
            y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task ResumeAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAsync<Session>(_fixture.TestSessionKey, It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.ResumeAsync<Session>(_fixture.TestSessionKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task ResumeAndGetAsync_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.ResumeAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task ResumeAndGetAsync_Should_Return_Success_With_Obtained_Session()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<Session>();
        var returnedSession = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAndGetAsync<Session>(session.Key, It.Is<SessionEntryOptions>(y =>
                y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                y.SlidingExpiration == expected.SlidingExpiration &&
                y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow),  cts.Token)).ReturnsAsync(returnedSession);

        // Act 
        var result = await _fixture.Service.ResumeAndGetAsync<Session>(session.Key, cts.Token);

        // Assert
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(returnedSession, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.RestoreAndGetAsync<Session>(session.Key, It.Is<SessionEntryOptions>(y =>
            y.AbsoluteExpiration == expected.AbsoluteExpiration &&
            y.SlidingExpiration == expected.SlidingExpiration &&
            y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow), cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task ResumeAndGetAsync_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var expected = _fixture.SettingsMock.Object.Value.GetSessionEntryOptions<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAndGetAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
                y.AbsoluteExpiration == expected.AbsoluteExpiration &&
                y.SlidingExpiration == expected.SlidingExpiration &&
                y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) , cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.ResumeAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.RestoreAndGetAsync<Session>(_fixture.TestSessionKey, It.Is<SessionEntryOptions>(y =>
            y.AbsoluteExpiration == expected.AbsoluteExpiration &&
            y.SlidingExpiration == expected.SlidingExpiration &&
            y.AbsoluteExpirationRelativeToNow == expected.AbsoluteExpirationRelativeToNow) ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task ResumeAndGetAsync_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.RestoreAndGetAsync<Session>(It.IsAny<string>(), It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.ResumeAndGetAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), cts.Token));
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_Success_With_Obtained_Lock_When_Exp_Passed()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var expected = new TimeSpan(1, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();

        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, expected, cts.Token)).ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, expected, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity, returnedLock.Object);

        _fixture.DataProviderMock.Verify(x => x.LockAsync<Session>(_fixture.TestSessionKey, expected ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_Success_With_Obtained_Lock_When_Exp_Null()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var def = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var returnedLock = new Mock<ISessionLock>();

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, null, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity, returnedLock.Object);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token), Times.Once);
    }

    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var def = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token))
            .ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, null, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task LockAsync_All_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task LockAsync_All_Should_Return_Success_With_Obtained_Lock()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var expiry = new TimeSpan(1, 2, 3, 4, 5);
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity, returnedLock.Object);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
    }

    [Fact]
    public async Task LockAsync_All_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var expiry = new TimeSpan(1, 2, 3, 4, 5);
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
            .ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
    }
    
    
    [Fact]
    public async Task LockAsync_All_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),It.IsAny<TimeSpan>(),It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), new TimeSpan());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
        [Fact]
    public async Task LockAsync_WaitRetry_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task LockAsync_WaitRetry_Should_Return_Success_With_Obtained_Lock()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity, returnedLock.Object);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task LockAsync_WaitRetry_Should_Return_Error_When_DataProvider_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token))
            .ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token),
            Times.Once);
    }
    
    
    [Fact]
    public async Task LockAsync_WaitRetry_Should_Return_ExceptionError_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),It.IsAny<TimeSpan>(),It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task GetLockedAsync_OnlyExp_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task GetLockedAsync_OnlyExp_Should_Return_Success_With_Obtained_Lock_When_Exp_Passed()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var expected = new TimeSpan(1, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, expected, cts.Token)).ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token)).ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, expected, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity.Lock, returnedLock.Object);
        Assert.Same(result.Entity.Session, returnedSession);

        _fixture.DataProviderMock.Verify(x => x.LockAsync<Session>(_fixture.TestSessionKey, expected ,cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task GetLockedAsync_OnlyExp_Should_Return_Success_With_Obtained_Lock_When_Exp_Null()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var def = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var returnedLock = new Mock<ISessionLock>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, null, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity.Lock, returnedLock.Object);
        Assert.Same(result.Entity.Session, returnedSession);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_Error_When_DataProvider_LockAsync_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var def = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token))
            .ReturnsAsync(error);
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, null, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
    }
    
    [Fact]
    public async Task LockAsync_OnlyExp_Should_Return_Error_When_DataProvider_GetAsync_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var def = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token))
            .ReturnsAsync(error);
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, null, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == def), cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
    }
    
    [Fact]
    public async Task GetLockedAsync_OnlyExp_Should_Return_ExceptionError_And_Release_Lock_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        var cts = _fixture.Cts;
        var @lock = new Mock<ISessionLock>();
        
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, ct: cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        
        @lock.Verify(x => x.DisposeAsync(), Times.Once);
    }
    
    [Fact]
    public async Task GetLockedAsync_All_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();

        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                new TimeSpan(), cts.Token));
    }
    
    [Fact]
    public async Task GetLockedAsync_All_Should_Return_Success_With_Obtained_Lock()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var expiry = new TimeSpan(1, 2, 3, 4, 5);
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity.Lock, returnedLock.Object);
        Assert.Same(result.Entity.Session, returnedSession);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_All_Should_Return_Error_When_DataProvider_LockAsync_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var expiry = new TimeSpan(1, 2, 3, 4, 5);
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
            .ReturnsAsync(error);
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
    }
    
    [Fact]
    public async Task LockAsync_All_Should_Return_Error_When_DataProvider_GetAsync_Returns_Error()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var expiry = new TimeSpan(1, 2, 3, 4, 5);
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();

        _fixture.DataProviderMock.Setup(x =>
                x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token))
            .ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, expiry, wait, retry, cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetLockedAsync_All_Should_Return_ExceptionError_And_Release_Lock_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        var cts = _fixture.Cts;
        var @lock = new Mock<ISessionLock>();
        
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));
        _fixture.DataProviderMock.Setup(x =>
            x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token)).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(),
            new TimeSpan(), new TimeSpan(), cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        
        @lock.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLockedAsync_WaitRetry_Should_Throw_When_CT_Cancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        cts.Cancel();

        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                cts.Token));
    }
    
    
    [Fact]
    public async Task GetLockedAsync_WaitRetry_Should_Return_Success_With_Obtained_Lock()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var exp = _fixture.SettingsMock.Object.Value.GetLockExpirationOrDefault<Session>();
        var wait = new TimeSpan(2, 2, 3, 4, 5);
        var retry = new TimeSpan(3, 2, 3, 4, 5);
        var returnedLock = new Mock<ISessionLock>();
        var returnedSession = _fixture.Session;

        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token)).ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));
        
        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Same(result.Entity.Lock, returnedLock.Object);
        Assert.Same(result.Entity.Session, returnedSession);

        _fixture.DataProviderMock.Verify(x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task LockAsync_WaitRetry_Should_Return_Error_When_DataProvider_LockAsync_Returns_Error()
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
                x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token))
            .ReturnsAsync(error);
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(Result<Session>.FromSuccess(returnedSession));

        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);

        _fixture.DataProviderMock.Verify(
            x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token),
            Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Never);
    }
    
    [Fact]
    public async Task LockAsync_WaitRetry_Should_Return_Error_When_DataProvider_GetAsync_Returns_Error()
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
            x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token)).ReturnsAsync(Result<ISessionLock>.FromSuccess(returnedLock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token))
            .ReturnsAsync(error);
        
        // Act 
        var result = await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, wait, retry, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.LockAsync<Session>(_fixture.TestSessionKey, It.Is<TimeSpan>(y => y == exp), wait, retry, cts.Token), Times.Once);
        _fixture.DataProviderMock.Verify(x => x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetLockedAsync_WaitRetry_Should_Return_ExceptionError_And_Release_Lock_When_Ex_Is_Caught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        var cts = _fixture.Cts;
        var @lock = new Mock<ISessionLock>();
        
        _fixture.DataProviderMock.Setup(x =>
            x.LockAsync<Session>(_fixture.TestSessionKey, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ISessionLock>.FromSuccess(@lock.Object));
        _fixture.DataProviderMock.Setup(x =>
                x.GetAsync<Session>(_fixture.TestSessionKey, cts.Token)).ThrowsAsync(ex);

        // Act 
        var result =
            await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
        
        @lock.Verify(x => x.DisposeAsync(), Times.Once);
    }
}
