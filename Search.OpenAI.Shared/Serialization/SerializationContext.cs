namespace Search.OpenAI.Shared.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ClientReceivableMessageBase))]
[JsonSerializable(typeof(ClientReceivableUserMessage))]
[JsonSerializable(typeof(ClientReceivableClearBufferMessage))]
[JsonSerializable(typeof(ClientSendableMessageBase))]
[JsonSerializable(typeof(ClientSendableConnectedMessage))]
[JsonSerializable(typeof(ClientSendableControlMessage))]
[JsonSerializable(typeof(ClientSendableSpeechStartedMessage))]
[JsonSerializable(typeof(ClientSendableTextDeltaMessage))]
[JsonSerializable(typeof(ClientSendableTranscriptionMessage))]
[JsonSerializable(typeof(SearchArgs))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(GroundingArgs))]
[JsonSerializable(typeof(GroundingData))]
public sealed partial class SerializationContext : JsonSerializerContext;
