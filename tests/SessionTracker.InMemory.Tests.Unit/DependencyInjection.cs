using System;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SessionTracker.Abstractions;

namespace SessionTracker.InMemory.Tests.Unit;

[UsedImplicitly]
public class DiFixture
{
    public IServiceProvider ServiceProvider { get; }

    public DiFixture()
    {
        var services = new ServiceCollection();
        services.AddSessionTracker()
            .AddInMemoryProviders(x =>
            {
                x.ShouldRegisterMemoryCache = true;
            });
        
        ServiceProvider = services.BuildServiceProvider();
    }
}

[CollectionDefinition("DependencyInjection")]
public class DependencyInjection : ICollectionFixture<DiFixture>
{
    [Collection("DependencyInjection")]
    public class ContainerShould(DiFixture fixture)
    {
        [Theory]
        [InlineData(typeof(IOptions<InMemorySessionTrackerSettings>))]
        [InlineData(typeof(TimeProvider))]
        [InlineData(typeof(MemoryCacheQueue))]
        [InlineData(typeof(IMemoryCache))]
        public void ResolveRequiredServices(Type serviceType)
        {
            // Arrange

            var provider = fixture.ServiceProvider;
            
            var resolveFunc = () => provider.GetRequiredService(serviceType);
            
            // Act && Assert
            resolveFunc.Should().NotThrow();
        }
        
        [Theory]
        [InlineData(typeof(ISessionLockProvider),typeof(InMemorySessionLockProvider))]
        [InlineData(typeof(ISessionDataProvider),typeof(InMemorySessionDataProvider))]
        public void ResolveCorrectServices(Type serviceType, Type implementationType)
        {
            // Arrange

            var provider = fixture.ServiceProvider;
            
            var resolveFunc = () => provider.GetRequiredService(serviceType);
            
            // Act && Assert
            resolveFunc.Should().NotThrow()
                .And
                .Subject.Invoke().Should().BeOfType(implementationType);
        }
    }
}