using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SessionTracker.Redis.Abstractions;

namespace SessionTracker.Redis.Tests.Unit;

[UsedImplicitly]
public class Fixture
{
    public IServiceProvider ServiceProvider { get; }

    public Fixture()
    {
        var services = new ServiceCollection();
        services.AddSessionTracker()
            .AddRedisProviders(x => {});
        
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
        [InlineData(typeof(TimeProvider))]
        [InlineData(typeof(IRedisConnectionMultiplexerProvider))]
        [InlineData(typeof(IDistributedLockFactoryProvider))]
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