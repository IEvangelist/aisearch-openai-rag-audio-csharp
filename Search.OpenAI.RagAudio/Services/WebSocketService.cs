namespace Search.OpenAI.RagAudio.Services;

public sealed class WebSocketService(
    IConfiguration configuration,
    ILogger<WebSocketService> logger) : IDisposable
{
    private readonly ClientWebSocket _client = new();

    public async Task ConnectAsync(
        Action onWebSocketOpen,
        Action onWebSocketClose,
        Action<Exception> onWebSocketError,
        Func<ClientSendableMessageBase?, Task> onWebSocketMessageAsync,
        CancellationToken cancellationToken)
    {
        var address = configuration["ApiAddress"];
        logger.LogInformation("API address: {Address}", address);

        var endpoint = configuration["RealtimeEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "RealtimeEndpoint is not configured.");
        }

        try
        {
            await _client.ConnectAsync(new Uri(endpoint), cancellationToken);

            if (_client is { State: WebSocketState.Open })
            {
                onWebSocketOpen.Invoke();
            }

            var buffer = new ArraySegment<byte>(new byte[2_048]);

            var bytesReceived = 0;

            while (!cancellationToken.IsCancellationRequested && _client is { State: WebSocketState.Open })
            {
                var result = await _client.ReceiveAsync(buffer, cancellationToken);

                if (result is { EndOfMessage: true })
                {
                    var finalResult = new WebSocketReceiveResult(bytesReceived + result.Count, result.MessageType, true, result.CloseStatus, result.CloseStatusDescription);

                    var json = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, finalResult.Count);

                    logger.LogInformation("Received: {Json}", json);

                    var message = JsonSerializer.Deserialize(
                        json, SerializationContext.Default.ClientSendableMessageBase);

                    await onWebSocketMessageAsync.Invoke(message);
                }
                else
                {
                    bytesReceived += result.Count;
                    buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + result.Count, buffer.Count - result.Count);
                }

                if (result.MessageType is WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            onWebSocketError.Invoke(exception);
        }
        finally
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);

            if (_client is { State: WebSocketState.Closed })
            {
                onWebSocketClose.Invoke();
            }
        }
    }

    public Task SendAudioBytesAsync(
        byte[] audioBytes,
        CancellationToken cancellationToken = default)
    {
        if (_client is not { State: WebSocketState.Open })
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "WebSocket connection isn't open.");
            }

            return Task.CompletedTask;
        }

        return _client.SendAsync(
            new ArraySegment<byte>(audioBytes),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            cancellationToken);
    }

    public Task SendJsonMessageAsync<T>(
        T message,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default) where T : ClientReceivableMessageBase
    {
        if (_client is not { State: WebSocketState.Open })
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "WebSocket connection isn't open.");
            }

            return Task.CompletedTask;
        }

        var json = JsonSerializer.Serialize(message, typeInfo);

        var buffer = Encoding.UTF8.GetBytes(json);

        return _client.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    void IDisposable.Dispose() => _client.Abort();
}
