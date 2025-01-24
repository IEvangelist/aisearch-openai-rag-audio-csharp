namespace Search.OpenAI.RagAudio.Services;

public sealed class WebSocketService(
    IConfiguration configuration,
    ILogger<WebSocketService> logger) : IDisposable
{
    private const int BufferSize = 2_048;
    private readonly ClientWebSocket _client = new();

    public async Task ConnectAsync(
        IRealtimeWebSocketHandler handler,
        CancellationToken cancellationToken)
    {
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
                handler.OnWebSocketOpen();
            }

            var sendAudioTask = SendAudioAsync(handler, cancellationToken);

            var buffer = new ArraySegment<byte>(new byte[BufferSize]);
            var bytesReceived = 0;

            while (!cancellationToken.IsCancellationRequested && _client is { State: WebSocketState.Open })
            {
                var result = await _client.ReceiveAsync(buffer, cancellationToken);

                // Handle text messages...
                if (result.MessageType is WebSocketMessageType.Text)
                {
                    if (result is { EndOfMessage: true })
                    {
                        var finalResult = new WebSocketReceiveResult(bytesReceived + result.Count, result.MessageType, true, result.CloseStatus, result.CloseStatusDescription);

                        var json = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, finalResult.Count);

                        logger.LogInformation("Received: {Json}", json);

                        var message = JsonSerializer.Deserialize(
                            json, SerializationContext.Default.ClientSendableMessageBase);

                        await handler.OnWebSocketMessageAsync(message);
                    }
                    else
                    {
                        bytesReceived += result.Count;
                        buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + result.Count, buffer.Count - result.Count);
                    }
                }

                // Handle audio...
                if (result.MessageType is WebSocketMessageType.Binary)
                {
                    if (result is { EndOfMessage: true })
                    {
                        var finalResult = new WebSocketReceiveResult(bytesReceived + result.Count, result.MessageType, true, result.CloseStatus, result.CloseStatusDescription);
                        var audioBytes = new byte[finalResult.Count];

                        Array.Copy(buffer.Array!, buffer.Offset, audioBytes, 0, finalResult.Count);

                        await handler.OnWebSocketAudioAsync(audioBytes);
                    }
                    else
                    {
                        bytesReceived += result.Count;
                        buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + result.Count, buffer.Count - result.Count);
                    }
                }

                // Close it all up...
                if (result.MessageType is WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            handler.OnWebSocketError(exception);
        }
        finally
        {
            if (_client is { State: WebSocketState.Open })
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
            }

            if (_client is { State: WebSocketState.Closed })
            {
                handler.OnWebSocketClose();
            }
        }
    }

    public async Task SendAudioAsync(
        IRealtimeWebSocketHandler handler,
        CancellationToken cancellationToken = default)
    {
        if (_client is not { State: WebSocketState.Open })
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("WebSocket connection isn't open.");
            }

            return;
        }

        var pipeReader = await handler.GetAudioReaderAsync(cancellationToken);
        if (pipeReader is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("The pipe reader is null, unable to send audio.");
            }

            return;
        }

        while (true)
        {
            var result = await pipeReader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;
            var sendSegment = new ArraySegment<byte>(buffer.ToArray());

            await _client.SendAsync(
                sendSegment,
                WebSocketMessageType.Binary,
                false,
                cancellationToken);

            pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                await _client.SendAsync(
                    new ArraySegment<byte>(),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);

                break;
            }
        }

        logger.LogInformation("Audio sent.");
    }

    public Task SendJsonMessageAsync<T>(
        T message,
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

        var json = JsonSerializer.Serialize(message, SerializationContext.Default.ClientReceivableMessageBase);

        var buffer = Encoding.UTF8.GetBytes(json);

        return _client.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    void IDisposable.Dispose() => _client.Abort();
}
