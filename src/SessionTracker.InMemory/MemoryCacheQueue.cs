using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace SessionTracker.InMemory;

/// <summary>
/// Memory cache wrap using a FIFO action queueing.
/// </summary>
[PublicAPI]
public class MemoryCacheQueue
{
    private Task _previousTask = Task.CompletedTask;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="memoryCache">Underlying memory cache.</param>
    public MemoryCacheQueue(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Adds an action to the queue.
    /// </summary>
    /// <param name="asyncFunction">The action to queue.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>A task representing the async operation.</returns>
    public async Task<T> EnqueueAsync<T>(Func<IMemoryCache,Task<T>> asyncFunction)
    {
        // see https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // get predecessor and wait until it's done. Also atomically swap in our own completion task.
        await Interlocked.Exchange(ref _previousTask, tcs.Task).ConfigureAwait(false);
        try
        {
            return await asyncFunction(_memoryCache).ConfigureAwait(false);
        }
        finally
        {
            tcs.SetResult();
        }
    }
    
    /*/// <summary>
    /// Adds an action to the queue.
    /// </summary>
    /// <param name="function">The action to queue.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>A task representing the async operation.</returns>
    public Task<T> Enqueue<T>(Func<IMemoryCache, T> function)
    {
        return Enqueue(x => Task.Run(() => function(_memoryCache)));
    }*/
    
    /// <summary>
    /// Adds an action to the queue.
    /// </summary>
    /// <param name="function">The action to queue.</param>
    /// <typeparam name="T">The result type.</typeparam>
    /// <returns>A task representing the async operation.</returns>
    public async Task<T> EnqueueAsync<T>(Func<IMemoryCache,T> function)
    {
        // see https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // get predecessor and wait until it's done. Also atomically swap in our own completion task.
        await Interlocked.Exchange(ref _previousTask, tcs.Task).ConfigureAwait(false);
        try
        {
            return function(_memoryCache);
        }
        finally
        {
            tcs.SetResult();
        }
    }
    
    /// <summary>
    /// Adds an action to the queue.
    /// </summary>
    /// <param name="asyncFunction">The action to queue.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnqueueAsync(Func<IMemoryCache,Task> asyncFunction)
    {
        // see https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // get predecessor and wait until it's done. Also atomically swap in our own completion task.
        await Interlocked.Exchange(ref _previousTask, tcs.Task).ConfigureAwait(false);
        try
        {
            await asyncFunction(_memoryCache).ConfigureAwait(false);
        }
        finally
        {
            tcs.SetResult();
        }
    }
    
    /// <summary>
    /// Adds an action to the queue.
    /// </summary>
    /// <param name="function">The action to queue.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task EnqueueAsync(Action<IMemoryCache> function)
    {
        // see https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // get predecessor and wait until it's done. Also atomically swap in our own completion task.
        await Interlocked.Exchange(ref _previousTask, tcs.Task).ConfigureAwait(false);
        try
        {
            function(_memoryCache);
        }
        finally
        {
            tcs.SetResult();
        }
    }
}