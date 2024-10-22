namespace Search.OpenAI.RagAudio.Types;

public record ResponseInputAudioTranscriptionCompleted(
    string Type,
    string EventId,
    string ItemId,
    int ContentIndex,
    string Transcript) : Message(Type);
