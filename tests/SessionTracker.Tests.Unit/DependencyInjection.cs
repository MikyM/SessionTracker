using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SessionTracker.Tests.Unit;

[UsedImplicitly]
public class Fixture
{
    public IServiceProvider ServiceProvider { get; }

    public Fixture()
    {
        var services = new ServiceCollection();
        services.AddSessionTracker();

        services.AddSingleton<ISessionLockProvider, DummyLockProvider>();
        services.AddSingleton<ISessionDataProvider, DummyDataProvider>();
        
        ServiceProvider = services.BuildServiceProvider();
    }
}

[CollectionDefinition("DependencyInjection")]
public class DependencyInjection : ICollectionFixture<Fixture>
{
    [Collection("DependencyInjection")]
    public class ContainerShould(Fixture fixture)
    {
        [Theory]
        [InlineData(typeof(ISessionTracker))]
        [InlineData(typeof(IOptions<SessionTrackerSettings>))]
        public void ResolveRequiredServices(Type serviceType)
        {
            // Arrange

            var provider = fixture.ServiceProvider;
            
            var resolveFunc = () => provider.GetRequiredService(serviceType);
            
            // Act && Assert
            resolveFunc.Should().NotThrow();
        }
    }
}