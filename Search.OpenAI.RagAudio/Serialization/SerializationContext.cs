namespace Search.OpenAI.RagAudio.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Content))]
[JsonSerializable(typeof(ExtensionMiddleTierToolResponse))]
[JsonSerializable(typeof(GroundingFile))]
[JsonSerializable(typeof(HistoryItem))]
[JsonSerializable(typeof(InputAudioBufferAppendCommand))]
[JsonSerializable(typeof(InputAudioBufferClearCommand))]
[JsonSerializable(typeof(InputAudioTranscription))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(Output))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(ResponseDone))]
[JsonSerializable(typeof(ResponseAudioDelta))]
[JsonSerializable(typeof(ResponseAudioTranscriptDelta))]
[JsonSerializable(typeof(ResponseInputAudioTranscriptionCompleted))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(SessionUpdateCommand))]
[JsonSerializable(typeof(Source))]
[JsonSerializable(typeof(ToolResult))]
[JsonSerializable(typeof(TurnDetection))]
public sealed partial class SerializationContext : JsonSerializerContext
{
}
