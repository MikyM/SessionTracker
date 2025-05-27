using JetBrains.Annotations;

namespace SessionTracker.InMemory;

/// <summary>
/// InMemory session tracker builder.
/// </summary>
[PublicAPI]
public class InMemorySessionTrackerBuilder
{
    /// <summary>
    /// The session tracker builder.
    /// </summary>
    public SessionTrackerBuilder SessionTrackerBuilder { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sessionTrackerBuilder"></param>
    public InMemorySessionTrackerBuilder(SessionTrackerBuilder sessionTrackerBuilder)
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