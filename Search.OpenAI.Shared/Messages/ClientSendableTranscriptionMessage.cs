namespace Search.OpenAI.Shared.Messages;

public record class ClientSendableTranscriptionMessage(
    string EventId,
    string Transcription) : ClientSendableMessageBase;
