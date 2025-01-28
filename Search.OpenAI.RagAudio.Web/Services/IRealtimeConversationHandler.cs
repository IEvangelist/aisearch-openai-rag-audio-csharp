namespace Search.OpenAI.RagAudio.Web.Services;

public interface IRealtimeConversationHandler
{
    public Task OnConversationUpdateAsync(ConversationUpdate? conversation);

    public Task OnAudioReceivedAsync(byte[] audioBytes);

    public Task OnTranscriptReadyAsync(string transcript);

    public Task OnConversationStatusAsync(RealtimeStatus status);

    public Task<PipeReader> GetAudioReaderAsync(CancellationToken cancellationToken);
}
