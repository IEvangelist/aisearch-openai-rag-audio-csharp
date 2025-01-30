namespace Search.OpenAI.RagAudio.Web.Serialization;

[JsonSourceGenerationOptions(
    defaults: JsonSerializerDefaults.Web,
    Converters = [ typeof(JsonStringEnumConverter<MediaDeviceKind>) ])]
[JsonSerializable(typeof(MediaDeviceInfo[]))]
[JsonSerializable(typeof(MediaDeviceKind))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(GroundingData))]
internal sealed partial class WebSerializerContext : JsonSerializerContext;
