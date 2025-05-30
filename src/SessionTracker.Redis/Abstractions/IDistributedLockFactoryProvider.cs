namespace SessionTracker.Redis.Abstractions;

/// <summary>
/// Represents a lock factory provider.
/// </summary>
[PublicAPI]
public interface IDistributedLockFactoryProvider
{
    /// <summary>
    /// Provides a Distributed Lock factory.
    /// </summary>
    /// <returns>An instance of <see cref="IDistributedLockFactory"/>.</returns>
    ValueTask<IDistributedLockFactory> GetDistributedLockFactoryAsync();    
}