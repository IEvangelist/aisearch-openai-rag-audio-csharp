namespace Search.OpenAI.RagAudio.Web.Models;

public record class SearchResult(
    string Title = "", 
    string Chunk = "", 
    string ChunkId = "");
