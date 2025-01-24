namespace Search.OpenAI.Shared.Messages;

[JsonDerivedType(typeof(ClientReceivableMessageBase), typeDiscriminator: "base")]
[JsonDerivedType(typeof(ClientReceivableUserMessage), typeDiscriminator: "user_message")]
[JsonDerivedType(typeof(ClientReceivableClearBufferMessage), typeDiscriminator: "clear_buffer")]
public record class ClientReceivableMessageBase
{
    public TimeOnly LocalTime { get; init; } = TimeOnly.FromDateTime(DateTime.Now);
}
