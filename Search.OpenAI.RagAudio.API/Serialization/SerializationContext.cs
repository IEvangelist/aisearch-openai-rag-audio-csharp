namespace Search.OpenAI.RagAudio.API.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SearchArgs))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(GroundingArgs))]
[JsonSerializable(typeof(GroundingData))]
public partial class SerializationContext : JsonSerializerContext
{
}
