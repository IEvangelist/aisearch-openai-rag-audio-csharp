namespace Search.OpenAI.Shared.Messages;

[JsonDerivedType(typeof(ClientSendableMessageBase), typeDiscriminator: "base")]
[JsonDerivedType(typeof(ClientSendableConnectedMessage), typeDiscriminator: "connected")]
[JsonDerivedType(typeof(ClientSendableControlMessage), typeDiscriminator: "control")]
[JsonDerivedType(typeof(ClientSendableSpeechStartedMessage), typeDiscriminator: "speech_started")]
[JsonDerivedType(typeof(ClientSendableTextDeltaMessage), typeDiscriminator: "text_delta")]
[JsonDerivedType(typeof(ClientSendableTranscriptionMessage), typeDiscriminator: "transcription")]
public record class ClientSendableMessageBase
{
    public TimeOnly LocalTime { get; init; } = TimeOnly.FromDateTime(DateTime.Now);
}
