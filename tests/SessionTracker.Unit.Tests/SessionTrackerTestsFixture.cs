using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Moq;
using SessionTracker.Abstractions;

namespace SessionTracker.Unit.Tests;

[UsedImplicitly]
public class SessionTrackerTestsFixture
{
    public readonly Mock<ISessionTrackerDataProvider> DataProviderMock = new();
    public readonly Mock<IOptions<SessionTrackerSettings>> SettingsMock = new();
    public Session Session => new(TestSessionKey);
    public string TestSessionKey => "test";
    public CancellationTokenSource Cts => new ();

    public SessionTrackerTestsFixture()
    {
        var opt = new SessionTrackerSettings();
        SettingsMock.Setup(x => x.Value).Returns(opt);
        Service = new(DataProviderMock.Object, SettingsMock.Object);
    }

    public SessionTracker Service { get; }

    public void Reset()
        => DataProviderMock.Reset();
}