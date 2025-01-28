namespace Search.OpenAI.RagAudio.Web.Models;

public sealed record class MediaDeviceInfo(
    string Label,
    string DeviceId,
    MediaDeviceKind Kind);
