namespace Search.OpenAI.RagAudio.Types;

public record ResponseDone(
    string Type,
    string EventId,
    Response Response) : Message(Type);
