namespace Search.OpenAI.Shared.Messages;

public record class ClientSendableControlMessage(string Action) : ClientSendableMessageBase;
