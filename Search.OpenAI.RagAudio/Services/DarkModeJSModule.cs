namespace Search.OpenAI.RagAudio.Services;

internal sealed partial class DarkModeJSModule
{
    [JSImport("setTheme", nameof(DarkModeJSModule))]
    public static partial void SetTheme(string? theme);
}
