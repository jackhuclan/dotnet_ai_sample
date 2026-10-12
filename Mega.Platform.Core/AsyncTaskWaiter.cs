using System.Collections.Concurrent;

namespace Mega.Platform.Core;

public class AsyncTaskWaiter : IAsyncTaskWaiter
{
    private readonly ConcurrentDictionary<string, AsyncTaskCompletionSource<byte[]>> waitingCalls;

    public AsyncTaskWaiter()
    {
        waitingCalls = new ConcurrentDictionary<string, AsyncTaskCompletionSource<byte[]>>();
    }

    public async Task<ArraySegment<byte>> ExecuteTaskAsWaiter(string waitingKey,
            Task waitableTask,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var awaitable = new AsyncTaskCompletionSource<byte[]>();

            if (!TryAdd(waitingKey, awaitable))
            {
                throw new InvalidOperationException();
            }

            await waitableTask.ConfigureAwait(false);

            using (cancellationToken.Register(() => awaitable.TrySetCanceled()))
            {
                return await awaitable.Task.ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            TryRemove(waitingKey);
        }
    }

    public bool TryGetValue(string waitingKey, out AsyncTaskCompletionSource<byte[]> awaitable)
    {
        return waitingCalls.TryGetValue(waitingKey, out awaitable);
    }

    public bool TryAdd(string waitingKey, AsyncTaskCompletionSource<byte[]> awaitable)
    {
        return waitingCalls.TryAdd(waitingKey, awaitable);
    }

    public bool TryRemove(string waitingKey)
    {
        return waitingCalls.TryRemove(waitingKey, out _);
    }

    public bool TrySetResult(string waitingKey, byte[] bytes)
    {
        if (waitingCalls.TryGetValue(waitingKey, out var awaitable))
        {
            return awaitable.TrySetResult(bytes);
        }

        return false;
    }

    public void TrySetCanceled(string waitingKey)
    {
        if (waitingCalls.TryGetValue(waitingKey, out var awaitable))
        {
            awaitable.TrySetCanceled();
        }
    }
}
