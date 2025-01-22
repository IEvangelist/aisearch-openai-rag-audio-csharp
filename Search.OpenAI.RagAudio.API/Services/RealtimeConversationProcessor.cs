using OpenAI.RealtimeConversation;

namespace Search.OpenAI.RagAudio.API.Services;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public sealed class RealtimeConversationProcessor(
    RealtimeConversationClient conversationClient) : IDisposable
{
    private RealtimeConversationSession? _session;

    void IDisposable.Dispose() => _session?.Dispose();

    public async Task ProcessAsync(
        HttpContext context,
        WebSocket serverWebSocket,
        CancellationToken cancellationToken = default)
    {
        _session ??= await conversationClient.StartConversationSessionAsync(cancellationToken);



        await Task.WhenAny(

            );
    }
}
