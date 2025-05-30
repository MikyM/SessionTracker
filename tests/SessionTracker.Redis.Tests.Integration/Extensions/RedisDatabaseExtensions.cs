namespace SessionTracker.Redis.Tests.Integration.Extensions;

internal static class RedisDatabaseExtensions
{
    public static async Task SetTestSessionAsync(this IDatabase database, string key, TestSession session)
    {
        await database.HashSetAsync(key, [ 
            new HashEntry(new RedisValue("data"), new RedisValue(session.ToString())),
            new HashEntry(new RedisValue("absexp"), new RedisValue(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)).ToUnixTimeSeconds().ToString())),
            new HashEntry(new RedisValue("sldexp"), new RedisValue("1234"))
        ]);
    }
}