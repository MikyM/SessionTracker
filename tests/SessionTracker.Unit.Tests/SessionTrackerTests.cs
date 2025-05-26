using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Remora.Results;
using SessionTracker.Abstractions;
using Xunit;

namespace SessionTracker.Unit.Tests;

[Trait("session-tracker", "base")]
public class SessionTrackerTests : IClassFixture<SessionTrackerTestsFixture>
{
    private readonly SessionTrackerTestsFixture _fixture;

    public SessionTrackerTests(SessionTrackerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.StartAsync(_fixture.Session, cts.Token));
    }
    
    [Fact]
    public async Task StartAsyncShouldPassSessionAndCT()
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
    public async Task StartAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task StartAsyncShouldPassProperEntryOptions()
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
    public async Task StartAsyncShouldReturnSuccess()
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
    public async Task StartAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task GetAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetAsyncShouldReturnSuccessWithObtainedSession()
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
    public async Task GetAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task GetAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task GetEvictedAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetFinishedAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    [Fact]
    public async Task GetEvictedAsyncShouldReturnSuccessWithObtainedSession()
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
    public async Task GetEvictedAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task GetEvictedAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task RefreshAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task RefreshAsyncShouldReturnSuccess()
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
    public async Task RefreshAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task UpdateAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAsync(_fixture.Session, cts.Token));
    }
    
    
    [Fact]
    public async Task UpdateAsyncShouldReturnSuccess()
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
    public async Task UpdateAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task UpdateAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task UpdateAndGetAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.UpdateAndGetAsync(_fixture.Session, cts.Token));
    }
    
    
    [Fact]
    public async Task UpdateAndGetAsyncShouldReturnSuccessWithObtainedSession()
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
    public async Task UpdateAndGetAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task UpdateAndGetAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task FinishAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task FinishAsyncShouldReturnSuccess()
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
        Assert.True(result.IsSuccess);

        _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, options ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAsyncShouldReturnErrorWhenDataProviderReturnsError()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var options = _fixture.SettingsMock.Object.Value.GetEvictionSessionEntryOptions<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.EvictAsync<Session>(_fixture.TestSessionKey, options , cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.EvictAsync<Session>(_fixture.TestSessionKey, options ,cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAsyncShouldReturnExceptionErrorWhenExIsCaught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.EvictAsync<Session>(_fixture.TestSessionKey, It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.FinishAsync<Session>(_fixture.TestSessionKey);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task FinishAndGetAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task FinishAndGetAsyncShouldReturnSuccessWithObtainedSession()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var session = _fixture.Session;
        var options = _fixture.SettingsMock.Object.Value.GetEvictionSessionEntryOptions<Session>();
        var returnedSession = _fixture.Session;
        
        _fixture.DataProviderMock.Setup(x =>
            x.EvictAndGetAsync<Session>(session.Key, options,  cts.Token)).ReturnsAsync(returnedSession);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(session.Key, cts.Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entity);
        Assert.Equal(returnedSession, result.Entity);
        
        _fixture.DataProviderMock.Verify(x => x.EvictAndGetAsync<Session>(session.Key, options, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAndGetAsyncShouldReturnErrorWhenDataProviderReturnsError()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        var error = new InvalidOperationError();
        var options = _fixture.SettingsMock.Object.Value.GetEvictionSessionEntryOptions<Session>();

        _fixture.DataProviderMock.Setup(x =>
            x.EvictAndGetAsync<Session>(_fixture.TestSessionKey, options,  cts.Token)).ReturnsAsync(error);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<InvalidOperationError>(result.Error);
        Assert.Same(error, result.Error);
        
        _fixture.DataProviderMock.Verify(x => x.EvictAndGetAsync<Session>(_fixture.TestSessionKey, options, cts.Token), Times.Once);
    }
    
    [Fact]
    public async Task FinishAndGetAsyncShouldReturnExceptionErrorWhenExIsCaught()
    {
        // Arrange
        _fixture.Reset();
        var ex = new InvalidOperationException();
        _fixture.DataProviderMock.Setup(x =>
            x.EvictAndGetAsync<Session>(It.IsAny<string>(), It.IsAny<SessionEntryOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(ex);

        // Act 
        var result = await _fixture.Service.FinishAndGetAsync<Session>(_fixture.TestSessionKey, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(ex, ((ExceptionError)result.Error!).Exception);
    }
    
    [Fact]
    public async Task ResumeAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.ResumeAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task ResumeAsyncShouldReturnSuccess()
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
    public async Task ResumeAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task ResumeAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task ResumeAndGetAsyncShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.ResumeAndGetAsync<Session>(_fixture.TestSessionKey, cts.Token));
    }
    
    
    [Fact]
    public async Task ResumeAndGetAsyncShouldReturnSuccessWithObtainedSession()
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
    public async Task ResumeAndGetAsyncShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task ResumeAndGetAsyncShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task LockAsyncOnlyExpShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), cts.Token));
    }
    
    [Fact]
    public async Task LockAsyncOnlyExpShouldReturnSuccessWithObtainedLockWhenExpPassed()
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
    public async Task LockAsyncOnlyExpShouldReturnSuccessWithObtainedLockWhenExpNull()
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
    public async Task LockAsyncOnlyExpShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task LockAsyncOnlyExpShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task LockAsyncAllShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task LockAsyncAllShouldReturnSuccessWithObtainedLock()
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
    public async Task LockAsyncAllShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task LockAsyncAllShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task LockAsyncWaitRetryShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.LockAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task LockAsyncWaitRetryShouldReturnSuccessWithObtainedLock()
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
    public async Task LockAsyncWaitRetryShouldReturnErrorWhenDataProviderReturnsError()
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
    public async Task LockAsyncWaitRetryShouldReturnExceptionErrorWhenExIsCaught()
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
    public async Task GetLockedAsyncOnlyExpShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();
        
        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), cts.Token));
    }
    
    
    [Fact]
    public async Task GetLockedAsyncOnlyExpShouldReturnSuccessWithObtainedLockWhenExpPassed()
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
    public async Task GetLockedAsyncOnlyExpShouldReturnSuccessWithObtainedLockWhenExpNull()
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
    public async Task LockAsyncOnlyExpShouldReturnErrorWhenDataProviderLockAsyncReturnsError()
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
    public async Task LockAsyncOnlyExpShouldReturnErrorWhenDataProviderGetAsyncReturnsError()
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
    public async Task GetLockedAsyncOnlyExpShouldReturnExceptionErrorAndReleaseLockWhenExIsCaught()
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
    public async Task GetLockedAsyncAllShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();

        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                new TimeSpan(), cts.Token));
    }
    
    [Fact]
    public async Task GetLockedAsyncAllShouldReturnSuccessWithObtainedLock()
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
    public async Task LockAsyncAllShouldReturnErrorWhenDataProviderLockAsyncReturnsError()
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
    public async Task LockAsyncAllShouldReturnErrorWhenDataProviderGetAsyncReturnsError()
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
    public async Task GetLockedAsyncAllShouldReturnExceptionErrorAndReleaseLockWhenExIsCaught()
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
    public async Task GetLockedAsyncWaitRetryShouldThrowWhenCTCancelled()
    {
        // Arrange
        _fixture.Reset();
        var cts = _fixture.Cts;
        await cts.CancelAsync();

        // Act && Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _fixture.Service.GetLockedAsync<Session>(_fixture.TestSessionKey, new TimeSpan(), new TimeSpan(),
                cts.Token));
    }
    
    
    [Fact]
    public async Task GetLockedAsyncWaitRetryShouldReturnSuccessWithObtainedLock()
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
    public async Task LockAsyncWaitRetryShouldReturnErrorWhenDataProviderLockAsyncReturnsError()
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
    public async Task LockAsyncWaitRetryShouldReturnErrorWhenDataProviderGetAsyncReturnsError()
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
    public async Task GetLockedAsyncWaitRetryShouldReturnExceptionErrorAndReleaseLockWhenExIsCaught()
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
