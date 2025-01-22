namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientSendableConnectedMessage(string Greeting)
    : IClientMessageDiscriminator, IClientSendableMessage
{
    string IClientMessageDiscriminator.Type { get; set; } = "connected";
}
