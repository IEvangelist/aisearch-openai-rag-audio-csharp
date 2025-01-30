namespace Search.OpenAI.RagAudio.Web.Services;

public sealed class AppJSModule(IJSRuntime js)
{
    private IJSObjectReference _module = null!;

    private async Task EnsureInitializeAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "../js/app.js");
    }

    internal async ValueTask<string?> SetAudioOutputAsync(ElementReference element, string? deviceId)
    {
        await EnsureInitializeAsync();

        var setDeviceId = await _module.InvokeAsync<string?>("setAudioOutputDevice", element, deviceId);

        return setDeviceId;
    }

    internal ValueTask<MediaDeviceInfo[]> GetClientSpeakersAsync()
    {
        return GetDevicesAsync("getClientSpeakers");
    }

    internal ValueTask<MediaDeviceInfo[]> GetClientMicrophonesAsync()
    {
        return GetDevicesAsync("getClientMicrophones");
    }

    private async ValueTask<MediaDeviceInfo[]> GetDevicesAsync(string identifier)
    {
        await EnsureInitializeAsync();

        var json = await _module.InvokeAsync<string>(identifier);

        var devices = JsonSerializer.Deserialize(json, WebSerializerContext.Default.MediaDeviceInfoArray);

        return devices ?? [];
    }
}
