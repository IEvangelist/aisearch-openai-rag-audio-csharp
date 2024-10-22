namespace Search.OpenAI.RagAudio.Types;

public record HistoryItem(
    string Id,
    string Transcript,
    GroundingFile[] GroundingFiles);
