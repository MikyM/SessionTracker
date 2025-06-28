[![NuGet](https://img.shields.io/nuget/v/SessionTracker)](https://www.nuget.org/packages/SessionTracker)[![NuGet](https://img.shields.io/nuget/dt/SessionTracker
)](https://www.nuget.org/packages/SessionTracker)
[![Build Status](https://github.com/MikyM/SessionTracker/actions/workflows/release.yml/badge.svg)](https://github.com/MikyM/SessionTracker/actions)
![GitHub License](https://img.shields.io/github/license/MikyM/SessionTracker)
[![Static Badge](https://img.shields.io/badge/Documentation-SessionTracker-Green)](https://mikym.github.io/SessionTracker)


# SessionTracker

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

For In-Memory implementation:
```csharp
builder.AddSessionTracker(options).AddInMemoryProviders(memoryOptions);
```

For Redis implementation:
```csharp
builder.AddSessionTracker(options).AddRedisProviders(redisOptions);
```
If you wish to configure the `JsonSerializerOptions` used for serializing/deserializing session instances within Redis provider use:
```csharp
builder.AddSessionTracker(options).AddRedisProviders(redisOptions => redisOptions.JsonSerializerConfiguration = ...);
```
or
```csharp
services.Configure<JsonSerializerOptions>(RedisSessionSettings.JsonSerializerName, yourOptions);
```


You can implement your own backing store provider and lock provider by implementing `ISessionTrackerDataProvider` or `ISessionLockProvder` interfaces respectively and registering your new services with the container manually or with helper methods like so:
```csharp
services.AddSessionTracker(options).AddDataProvider<YourDataProviderType>();
services.AddSessionTracker(options).AddLockProvider<YourLockProviderType>();

// or 

services.AddSessionTracker(options).AddDataProvider(AnInstanceOfYourDataProvider);
services.AddSessionTracker(options).AddLockProvider(AnInstanceOfYourLockProvider);
```
or register different implementations of suitable interfaces yourself.

These will overwrite any other provider implementation currently registered with the container.

## Documentation

Documentation available at https://mikym.github.io/SessionTracker.

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

    public async Task HandleAsync()
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

    public async Task HandleAsync()
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

    public async Task HandleAsync()
    {
        await _tracker.FinishAsync<CustomSession>("superKeyForThisSession");
    }
}
```
