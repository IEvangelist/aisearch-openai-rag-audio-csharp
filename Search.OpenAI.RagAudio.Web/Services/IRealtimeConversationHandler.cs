namespace Search.OpenAI.RagAudio.Web.Services;

public interface IRealtimeConversationHandler
{
    public Task OnConversationUpdateAsync(ConversationUpdate? conversation);

    public Task OnPlayAudioAsync(byte[] audioBytes);

    public Task<PipeReader> GetAudioReaderAsync(CancellationToken cancellationToken);
}
