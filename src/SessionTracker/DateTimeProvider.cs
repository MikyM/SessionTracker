namespace SessionTracker;

internal class DateTimeProvider
{
    private static DateTimeProvider _current = new();

    public static DateTimeProvider Instance => _current;

    public virtual DateTime UtcNow => DateTime.UtcNow;
    public virtual DateTime Now => DateTime.Now;
    
    public virtual DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;
    public virtual DateTimeOffset OffsetNow => DateTimeOffset.Now;
    
    /// <summary>
    /// Sets a provider to be used to retrieve datetimes.
    /// </summary>
    /// <param name="provider">Provider to use.</param>
    public static void SetProvider(DateTimeProvider provider) => _current = provider;
    
    /// <summary>
    /// Resets the internal provider to default.
    /// </summary>
    public static void ResetToDefault() => _current = new DateTimeProvider();
}
