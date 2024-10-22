namespace Search.OpenAI.RagAudio.API.Realtime;

internal struct RealtimeMessage(JsonNode Node)
{
    private readonly JsonNode? _item = Node["item"];

    public readonly string? ItemType => _item?["type"]?.ToString();

    public readonly string? ToolCallId => _item?["call_id"]?.ToString();

    public readonly string? ToolCallName => _item?["name"]?.ToString();

    public readonly string? ToolCallArguments => _item?["arguments"]?.ToString();

    public readonly bool TrimToolResponses()
    {
        var trimmed = false;

        if (Node["response"] is JsonObject response &&
            response["output"] is JsonArray output)
        {
            for (var i = output.Count - 1; i >= 0; i--)
            {
                if (output[i] is JsonObject entry &&
                    (string?)entry["type"] == "function_call")
                {
                    output.RemoveAt(i);
                    trimmed = true;
                }
            }
        }

        return trimmed;
    }

    // Since we won't need to fully read or transform most messages, we use a streaming check to extract
    // the message type, and take the hit to restart parsing for the cases where we need them. In particular
    // this enables us to quickly skip all audio payloads.
    public static string GetMessageType(MemoryStream message)
    {
        try
        {
            var reader = new Utf8JsonReader(new ReadOnlySpan<byte>(message.GetBuffer(), 0, (int)message.Length));

            while (reader.Read())
            {
                if (reader.TokenType is JsonTokenType.PropertyName && reader.GetString() == "type")
                {
                    reader.Read();
                    if (reader.TokenType is JsonTokenType.String)
                    {
                        return reader.GetString() ?? string.Empty;
                    }
                    break;
                }
            }

            return string.Empty;
        }
        finally
        {
            message.Position = 0;
        }
    }
}