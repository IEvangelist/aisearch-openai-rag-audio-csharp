namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientSendableTranscriptionMessage(string EventId, string Transcription)
    : IClientMessageDiscriminator, IClientSendableMessage
{
    string IClientMessageDiscriminator.Type { get; set; } = "transcription";
}
