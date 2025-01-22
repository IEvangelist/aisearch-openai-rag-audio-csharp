namespace Search.OpenAI.Shared.Messages;

public sealed record class ClientSendableSpeechStartedMessage
    : IClientMessageDiscriminator, IClientSendableMessage
{
    string IClientMessageDiscriminator.Type { get; set; } = "speech_started";
}
