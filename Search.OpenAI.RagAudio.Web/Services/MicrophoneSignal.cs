namespace Search.OpenAI.RagAudio.Web.Services;

public sealed class MicrophoneSignal
{
    private readonly TaskCompletionSource<bool> _microphoneReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task WaitForMicrophoneAvailabilityAsync(CancellationToken cancellationToken)
    {
        var cancellationTask = new TaskCompletionSource<bool>();

        using (cancellationToken.Register(() => cancellationTask.TrySetCanceled(), useSynchronizationContext: false))
        {
            var completedTask = await Task.WhenAny(_microphoneReady.Task, cancellationTask.Task);

            await completedTask;
        }
    }

    public void MicrophoneAvailable()
    {
        _microphoneReady.TrySetResult(true);
    }
}
