namespace Search.OpenAI.RagAudio.API.Services;

internal readonly record struct ProcessorResult(
    MemoryStream? Forward,
    MemoryStream? Backward = null);