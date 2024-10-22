namespace Search.OpenAI.RagAudio.Types;

public record ExtensionMiddleTierToolResponse(
    string Type,
    string PreviousItemId,
    string ToolName,
    string ToolResult) : Message(Type);
