namespace SessionTracker.Tests.Shared;

public static class SharedHelpers
{
    public static TestSession CreateSession() => new(Guid.NewGuid().ToString());
    
    public static TestSession CreateSession(string key) => new(key);
}