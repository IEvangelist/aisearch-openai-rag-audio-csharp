namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientReceivableUserMessage(string Text) : IClientMessageDiscriminator
{
    string IClientMessageDiscriminator.Type { get; set; } = "user_message";
}
