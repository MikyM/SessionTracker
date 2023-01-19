# ResultCommander

[![Build Status](https://github.com/MikyM/ResultCommander/actions/workflows/release.yml/badge.svg)](https://github.com/MikyM/ResultCommander/actions)

Library featuring a command handler pattern for both synchronous and asynchronous operations.

Uses a functional [Result](https://github.com/Remora/Remora.Results) approach for failure-prone operations.

To utilize all features using Autofac is required. 

## Features

- Synchronous and asynchronous command handler definitions
- Definitions and base implementations of commands
- Supports decorators and adapters via Autofac's methods

## Description

There are two command types - one that only returns a [Result](https://github.com/Remora/Remora.Results) and one that returns an additional entity contained within the [Result](https://github.com/Remora/Remora.Results).

Every handler must return a [Result](https://github.com/Remora/Remora.Results) struct which determines whether the operation succedeed or not, handlers may or may not return additional results contained within the Result struct - this is defined by the handled comand.

## Installation

To register handlers with the DI container use the `ContainerBuilder` or `IServiceCollection` extension methods provided by the library:

```csharp
builder.AddResultCommander(assembliesToScan);
```

To register decorators or adapters use the methods available on ResultCommanderConfiguration like so:
```csharp
builder.AddResultCommander(assembliesToScan, options => 
{
    options.AddDecorator<FancyDecorator, ISyncCommandHandler<SimpleCommand>();
});
```
You can register multiple decorators and they'll be applied in the order that you register them - read more at [Autofac's docs regarding decorators and adapters](https://autofac.readthedocs.io/en/latest/advanced/adapters-decorators.html).

## Example usage

<b> You should never throw exceptions from within handlers, they should be exception free - instead return appropriate error results (and catch possible exceptions).</b> Library offers a simple error catching, logging and exception to result error decorators for uses where writing try-catch blocks becomes a pain - but remember that these results will never be as informative as manually returned proper result error types.

A command without a concrete result:
```csharp
public SimpleCommand : ICommand
{
    public bool IsSuccess { get; }
    
    public SimpleCommand(bool isSuccess = true)
        => IsSuccess = isSuccess;
}
```

And a synchronous handler that handles it:
```csharp
public SimpleSyncCommandHandler : ISyncCommandHandler<SimpleCommand>
{
    Result Handle(SimpleCommand command)
    {
        if (command.IsSuccess)
            return Result.FromSuccess();
            
        return new InvalidOperationError();
    }
}
```

A command with a concrete result:
```csharp
public SimpleCommandWithConcreteResult : ICommand<int>
{
    public bool IsSuccess { get; }
    
    public SimpleCommand(bool isSuccess = true)
        => IsSuccess = isSuccess;
}
```

And a synchronous handler that handles it:
```csharp
public SimpleSyncCommandHandlerWithConcreteResult : ISyncCommandHandler<SimpleCommand, int>
{
    Result<int> Handle(SimpleCommand command)
    {
        if (command.IsSuccess)
            return 1;
            
        return new InvalidOperationError();
    }
}
```