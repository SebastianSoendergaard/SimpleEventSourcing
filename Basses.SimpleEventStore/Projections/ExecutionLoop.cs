namespace Basses.SimpleEventStore.Projections;

internal class ExecutionLoop
{
    private Task? _loop;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void Start(TimeSpan loopDelay, Func<CancellationToken, Task> onExecute, Action<Exception?> onError)
    {
        try
        {
            _loop = new Task(async () =>
            {
                while (!_cancellationTokenSource.Token.WaitHandle.WaitOne(loopDelay))
                {
                    await onExecute(_cancellationTokenSource.Token);
                }
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            }, _cancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning);

            _loop.ContinueWith(x =>
            {
                onError(x.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);

            _loop.Start();
        }
        catch (Exception ex)
        {
            onError(ex);
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _loop?.Wait();
    }
}
