namespace Search.OpenAI.Shared.Messages;

public record class ClientReceivableUserMessage(string Text) : ClientReceivableMessageBase;