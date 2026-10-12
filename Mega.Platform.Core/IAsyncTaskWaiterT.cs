namespace Mega.Platform.Core;

public interface IAsyncTaskWaiter<T>
{
    Task<T> ExecuteTaskAsWaiter(string waitingKey,
        Task waitableTask,
        CancellationToken cancellationToken = default);

    bool TryAdd(string waitingKey, AsyncTaskCompletionSource<T> awaitable);

    bool TryGetValue(string waitingKey, out AsyncTaskCompletionSource<T> awaitable);

    void TrySetCanceled(string waitingKey);

    bool TryRemove(string waitingKey);

    bool TrySetResult(string waitingKey, T bytes);
}
