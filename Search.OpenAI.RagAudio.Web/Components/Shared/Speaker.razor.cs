namespace Search.OpenAI.RagAudio.Web.Components.Shared;

public sealed partial class Speaker(IJSRuntime js, ILogger<Speaker> logger)
{
    private IJSObjectReference? _module;
    private IJSObjectReference? _speaker;

    private long _bytesProcessed = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (_module is null)
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", "../js/speaker.js");

            logger.LogInformation("Initialized speaker module.");
        }
    }

    public async Task<bool> EnqueueAsync(byte[]? audioData)
    {
        if (_module is null)
        {
            logger.LogWarning("Speaker module is null");

            return false;
        }

        if (audioData is null or { Length: 0 })
        {
            logger.LogWarning("Null or empty audio data.");

            return false;
        }

        if (_speaker is null)
        {
            _speaker = await _module.InvokeAsync<IJSObjectReference>("start");

            logger.LogInformation("Initialized speaker object.");

        }
        var enqueued = false;

        await InvokeAsync(async () =>
        {
            try
            {
                await _speaker.InvokeVoidAsync("enqueue", audioData);

                _bytesProcessed += audioData.Length;

                enqueued = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enqueuing audio data");

                _ = DispatchExceptionAsync(ex);

                enqueued = false;
            }
            finally
            {
                StateHasChanged();
            }
        });

        return enqueued;
    }

    public async Task ClearPlaybackAsync()
    {
        await (_speaker?.InvokeVoidAsync("clear") ?? ValueTask.CompletedTask);
    }
}
