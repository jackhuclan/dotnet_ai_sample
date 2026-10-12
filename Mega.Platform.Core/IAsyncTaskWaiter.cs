namespace Mega.Platform.Core;

public interface IAsyncTaskWaiter
{
    Task<ArraySegment<byte>> ExecuteTaskAsWaiter(string waitingKey,
        Task waitableTask,
        CancellationToken cancellationToken = default);
    bool TryAdd(string waitingKey, AsyncTaskCompletionSource<byte[]> awaitable);
    bool TryGetValue(string waitingKey, out AsyncTaskCompletionSource<byte[]> awaitable);
    void TrySetCanceled(string waitingKey);
    bool TryRemove(string waitingKey);
    bool TrySetResult(string waitingKey, byte[] bytes);
}
