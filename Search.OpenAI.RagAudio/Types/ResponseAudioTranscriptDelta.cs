namespace Search.OpenAI.RagAudio.Types;

public record ResponseAudioTranscriptDelta(
    string Type,
    string Delta) : Message(Type);
