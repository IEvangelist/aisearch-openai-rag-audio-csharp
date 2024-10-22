namespace Search.OpenAI.RagAudio.API.Realtime;

internal static class RealtimeUriExtensions
{
    internal static Uri ToRealtimeUri(this Uri serverBaseUri, string deployment) =>
        new UriBuilder(serverBaseUri)
        {
            Path = "/openai/realtime",
            Query = $"api-version=2024-10-01-preview&deployment={deployment}"
        }.Uri;
}
