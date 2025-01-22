namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientSendableControlMessage(string Action)
    : IClientMessageDiscriminator, IClientSendableMessage
{
    string IClientMessageDiscriminator.Type { get; set; } = "control";
}
