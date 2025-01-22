namespace Search.OpenAI.Shared.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(IClientMessageDiscriminator))]
[JsonSerializable(typeof(ClientReceivableUserMessage))]
[JsonSerializable(typeof(ClientSendableConnectedMessage))]
[JsonSerializable(typeof(ClientSendableControlMessage))]
[JsonSerializable(typeof(ClientSendableSpeechStartedMessage))]
[JsonSerializable(typeof(ClientSendableTextDeltaMessage))]
[JsonSerializable(typeof(ClientSendableTranscriptionMessage))]
public sealed partial class SerializationContext : JsonSerializerContext;
