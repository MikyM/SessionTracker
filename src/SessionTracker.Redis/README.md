# SessionTracker

[![Build Status](https://github.com/MikyM/SessionTracker/actions/workflows/release.yml/badge.svg)](https://github.com/MikyM/SessionTracker/actions)

Library that allows working with distributed "sessions".

## Features

- Session tracking via starting, updating, finishing, fetching and fetching finished `Session` objects.
- Configurable caching settings per `Session` type.
- Redis implementations provided out of the box.

## Description

A session is encapsulated data that has to be passed around a distributed cache or any other backing-store custom implementation. It is helpful when dealing with for example Discord API interactions that may need some data passed between them without relying on Discords custom ID.

## Installation

To register the session tracker service use the following extension methods:

For Redis implementation:
```csharp
builder.AddRedisSessionTracker(options, redisOptions);
```
If you wish to configure the `JsonSerializerOptions` used for serializing/deserializing session instances within Redis provider use:
```csharp
services.Configure<JsonSerializerOptions>(SessionSettings.JsonSerializerName, yourOptions);
```

You can implement your own backing store provider by implementing `ISessionsBackingStoreProvider` interface and registering your new service with the container like so:
```csharp
services.AddSingleton<ISessionsBackingStoreProvider, YourImplementationType>;
```

## Example usage

Create your own `Session` type:
```csharp
public CustomSession : Session
{
    public bool IsThisASuperSession { get; set; }
    
    public CustomSession(bool isSuper = true)
        => IsThisASuperSession = isSuper;
}
```

Inject `ISessionTracker` to your handlers/services/controllers/whatnot:

Start session inside one handler:
```csharp
public FirstSimpleInteractionHandler
{
    private readonly ISessionTracker _tracker;

    public FirstSimpleInteractionHandler(ISessionTracker tracker)
        => _tracker = tracker;

    void Handle()
    {
        string key = "superKeyForThisSession";
        var session = new CustomSession(false);

        var lockResult = await _tracker.StartSessionAsync<CustomSession>(key, session);
        if (!lockResult.IsSuccess)
            return;

        await using var @lock = lockResult.Entity;
    }
}
```

Update from another:
```csharp
public SecondSimpleInteractionHandler
{
    private readonly ISessionTracker _tracker;

    public SecondSimpleInteractionHandler(ISessionTracker tracker)
        => _tracker = tracker;

    void Handle()
    {
        string key = "superKeyForThisSession"

        var lockedResult = await _tracker.GetSessionAsync<CustomSession>(key);
        if (!lockedResult.IsDefined(out var lockedSession))
            return;

        await using var @lock = lockedSession.Lock;

        lockedSession.Session.IsThisASuperSession = false;

        await _tracker.UpdateSessionAsync<CustomSession>(key, lockedSession.Session);
    }
}
```

Use in third:
```csharp
public ThirdSimpleInteractionHandler
{
    private readonly ISessionTracker _tracker;

    public ThirdSimpleInteractionHandler(ISessionTracker tracker)
        => _tracker = tracker;

    void Handle()
    {
        string key = "superKeyForThisSession"

        var result = await _tracker.GetBareSessionAsync<CustomSession>(key);
        if (!result.IsDefined(out var session))
            return;

        var check = result.IsThisASuperSession // returns false
    }
}
```

Finalize in fourth:
```csharp
public FourthSimpleInteractionHandler
{
    private readonly ISessionTracker _tracker;

    public FourthSimpleInteractionHandler(ISessionTracker tracker)
        => _tracker = tracker;

    void Handle()
    {
        string key = "superKeyForThisSession"
        var lockResult = await _tracker.LockAsync<CustomSession>(key);
        if (!lockResult.IsSuccess)
            return;

        await using var @lock = lockResult.Entity;

        await _tracker.FinishSessionAsync<CustomSession>(key);
    }
}
```

Get from finalized cache in fifth:
```csharp
public FifthSimpleInteractionHandler
{
    private readonly ISessionTracker _tracker;

    public FifthSimpleInteractionHandler(ISessionTracker tracker)
        => _tracker = tracker;

    void Handle()
    {
        string key = "superKeyForThisSession"
        
        // note that using GetSessionAsync here would result in a NotFoundError
        var result = await _tracker.GetFinishedSessionAsync<CustomSession>(key);
        if (!result.IsDefined(out var session))
            return;
    }
}
```