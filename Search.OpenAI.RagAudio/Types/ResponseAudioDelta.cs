namespace Search.OpenAI.RagAudio.Types;

public record ResponseAudioDelta(
    string Type,
    string Delta) : Message(Type);
