namespace Search.OpenAI.Shared.Messages;

public record class ClientSendableTextDeltaMessage(
    string Delta,
    string ContentId) : ClientSendableMessageBase;
