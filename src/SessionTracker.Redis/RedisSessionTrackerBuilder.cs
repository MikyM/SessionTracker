using JetBrains.Annotations;

namespace SessionTracker.Redis;

/// <summary>
/// Redis session tracker builder.
/// </summary>
[PublicAPI]
public class RedisSessionTrackerBuilder
{
    /// <summary>
    /// The session tracker builder.
    /// </summary>
    public SessionTrackerBuilder SessionTrackerBuilder { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sessionTrackerBuilder"></param>
    public RedisSessionTrackerBuilder(SessionTrackerBuilder sessionTrackerBuilder)
    {
        SessionTrackerBuilder = sessionTrackerBuilder;
    }

    
    /// <summary>
    /// Returns the main session tracker builder.
    /// </summary>
    /// <returns>The instance of <see cref="SessionTrackerBuilder"/></returns>
    public SessionTrackerBuilder Complete()
        => SessionTrackerBuilder;
}