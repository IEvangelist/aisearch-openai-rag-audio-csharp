namespace Search.OpenAI.RagAudio.Web.Serialization;

[JsonSourceGenerationOptions(
    defaults: JsonSerializerDefaults.Web,
    Converters = [ typeof(JsonStringEnumConverter<MediaDeviceKind>) ])]
[JsonSerializable(typeof(MediaDeviceInfo[]))]
[JsonSerializable(typeof(MediaDeviceKind))]
internal sealed partial class WebSerializerContext : JsonSerializerContext;
