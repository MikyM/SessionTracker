using JetBrains.Annotations;

namespace SessionTracker.DistributedLock;

/// <summary>
/// DistributedLock session tracker builder.
/// </summary>
[PublicAPI]
public class DistributedLockSessionTrackerBuilder
{
    /// <summary>
    /// The session tracker builder.
    /// </summary>
    public SessionTrackerBuilder SessionTrackerBuilder { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="sessionTrackerBuilder"></param>
    public DistributedLockSessionTrackerBuilder(SessionTrackerBuilder sessionTrackerBuilder)
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