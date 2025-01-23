namespace Search.OpenAI.Shared.Search;

public record class SearchResult(
    string Title = "", 
    string Chunk = "", 
    string ChunkId = "");
