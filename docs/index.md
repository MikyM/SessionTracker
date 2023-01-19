# SessionTracker

[![Build Status](https://github.com/MikyM/SessionTracker/actions/workflows/release.yml/badge.svg)](https://github.com/MikyM/SessionTracker/actions)

Library that allows working with distributed "sessions".

## Features

- Session tracking via starting, updating, finishing, resuming, locking and fetching finished `Session` objects.
- Configurable caching settings per `Session` type.
- Redis implementations provided out of the box.

## Description

A session is encapsulated data that has to be passed around a distributed cache or any other backing-store custom implementation. It is helpful when dealing with for example Discord API interactions that may need some data passed between them while there are multiple service instances.

## Installation

To register the session tracker service use the following extension methods:

For base services with no backing-store implementation registration:
```csharp
builder.AddSessionTracker(options);
```

For Redis implementation:
```csharp
builder.AddRedisSessionTracker(redisOptions, options);
```
If you wish to configure the `JsonSerializerOptions` used for serializing/deserializing session instances within Redis provider use:
```csharp
services.Configure<JsonSerializerOptions>(RedisSessionSettings.JsonSerializerName, yourOptions);
```

You can implement your own backing store provider and lock provider by implementing `ISessionTrackerDataProvider` or `ISessionLockProvder` interfaces respectively and registering your new services with the container like so:
```csharp
services.AddSessionTrackerDataProvider<YourDataProviderType>();
services.AddSessionTrackerLockProvider<YourLockProviderType>();

// or 

services.AddSessionTrackerDataProvider(AnInstanceOfYourDataProvider);
services.AddSessionTrackerLockProvider(AnInstanceOfYourLockProvider);
```

These will overwrite any other provider implementation currently registered with the container.

## Example usage

Create your own `Session` type:
```csharp
public CustomSession : Session
{
    public bool IsThisASuperSession { get; set; }
    
    public CustomSession(string key, bool isSuper = true) : base(key)
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
        var session = new CustomSession("superKeyForThisSession", false);

        var result = await _tracker.StartAsync(session);
        if (!result.IsSuccess)
            return;
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
        var result = await _tracker.GetLockedAsync<CustomSession>("superKeyForThisSession");
        if (!result.IsDefined(out var lockedSession))
            return;

        await using var @lock = lockedSession.Lock;

        lockedSession.Session.IsThisASuperSession = true;

        await _tracker.UpdateSessionAsync(lockedSession.Session);
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
        await _tracker.FinishAsync<CustomSession>("superKeyForThisSession");
    }
}
```
