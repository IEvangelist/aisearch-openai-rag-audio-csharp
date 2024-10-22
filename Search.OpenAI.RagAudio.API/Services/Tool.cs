namespace Search.OpenAI.RagAudio.API.Services;

internal readonly record struct Tool(
    string Name,
    object Schema,
    ToolResultDestination Destination,
    Func<string?, Task<string?>> Target)
{
    internal readonly JsonNode SchemaJsonNode { get; } =
        // Expensive round trip, but we only do this once during start up
        JsonNode.Parse(JsonSerializer.Serialize(Schema))!;
}