namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientSendableTextDeltaMessage(string Delta, string ContentId)
    : IClientMessageDiscriminator, IClientSendableMessage
{
    string IClientMessageDiscriminator.Type { get; set; } = "text_delta";
}
