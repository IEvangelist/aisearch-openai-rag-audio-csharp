namespace Search.OpenAI.RagAudio.Services;

public interface IRealtimeWebSocketHandler
{
    public void OnWebSocketOpen();

    public void OnWebSocketClose();

    public void OnWebSocketError(Exception exception);

    public Task OnWebSocketMessageAsync(ClientSendableMessageBase? message);

    public Task OnWebSocketAudioAsync(byte[] audioBytes);

    public Task<PipeReader> GetAudioReaderAsync(CancellationToken cancellationToken);
}
