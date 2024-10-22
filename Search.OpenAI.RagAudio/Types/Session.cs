namespace Search.OpenAI.RagAudio.Types;

public record Session(
    TurnDetection? TurnDetection,
    InputAudioTranscription? InputAudioTranscription = null);
