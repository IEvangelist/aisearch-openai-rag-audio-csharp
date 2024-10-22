namespace Search.OpenAI.RagAudio.API.Services;

internal sealed class WebSocketForwarder
{
    private const int MaxChannelSize = 16;

    internal async Task ForwardWhileCommunicatingAsync(
        ClientWebSocket clientWebSocket,
        WebSocket serverWebSocket,
        Func<MemoryStream, Task<ProcessorResult>> onProcessClientMessage,
        Func<MemoryStream, Task<ProcessorResult>>  onProcessServerMessage,
        CancellationToken cancellationToken)
    {
        var sessionState = new SessionState(IsPendingTools: true);

        var @in = Channel.CreateBounded<MemoryStream>(MaxChannelSize);
        var @out = Channel.CreateBounded<MemoryStream>(MaxChannelSize);

        var clientToServer = ForwardToChannelAsync(clientWebSocket, @in, @out, onProcessClientMessage, cancellationToken);
        var fromServer = ForwardFromChannelAsync(@in, serverWebSocket, cancellationToken);

        var serverToClient = ForwardToChannelAsync(serverWebSocket, @out, @in, onProcessServerMessage, cancellationToken);
        var fromClient = ForwardFromChannelAsync(@out, clientWebSocket, cancellationToken);

        await Task.WhenAny(clientToServer, fromServer, serverToClient, fromClient);
    }

    private static async Task ForwardToChannelAsync(
        WebSocket webSocket,
        Channel<MemoryStream> channel,
        Channel<MemoryStream> backwardChannel,
        Func<MemoryStream, Task<ProcessorResult>> onMessageAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in ReceiveMessagesAsync(webSocket, cancellationToken))
            {
                var result = await onMessageAsync.Invoke(message);
                if (result.Forward is not null)
                {
                    await channel.Writer.WriteAsync(result.Forward, cancellationToken);
                }
                if (result.Backward is not null)
                {
                    await backwardChannel.Writer.WriteAsync(result.Backward, cancellationToken);
                }
            }
        }
        finally
        {
            channel.Writer.Complete();
        }
    }

    private async static Task ForwardFromChannelAsync(
        Channel<MemoryStream> channel,
        WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await webSocket.SendAsync(
                buffer: new ReadOnlyMemory<byte>(message.GetBuffer(), 0, (int)message.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        }
    }

    private static async IAsyncEnumerable<MemoryStream> ReceiveMessagesAsync(
        WebSocket webSocket,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 8];
        MemoryStream? stream = new();
        while (true)
        {
            var r = await webSocket.ReceiveAsync(buffer, cancellationToken);
            if (r.MessageType is WebSocketMessageType.Close)
            {
                yield break;
            }

            stream ??= new(r.Count);

            stream.Write(buffer, 0, r.Count);

            if (r.EndOfMessage)
            {
                yield return stream;

                stream = null;
            }
        }
    }
}
