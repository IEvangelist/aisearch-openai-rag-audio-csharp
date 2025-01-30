namespace Search.OpenAI.Shared.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SearchArgs))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(GroundingArgs))]
[JsonSerializable(typeof(GroundingData))]
public sealed partial class SharedSerializerContext : JsonSerializerContext;
