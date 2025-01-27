namespace Search.OpenAI.RagAudio.Web.Extensions;

internal static class ConfigurationExtensions
{
    internal static Uri GetServiceUri(this IConfiguration configuration, string connectionName, string uriScheme = "https")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(uriScheme);

        var serviceKey = $"services:{connectionName}:{uriScheme}:0";

        var serviceUri = configuration.GetValue<string>(serviceKey);

        if (serviceUri is null)
        {
            throw new InvalidOperationException(
                $"Service URI not found for connection name '{connectionName}' and URI scheme '{uriScheme}'.");
        }

        return new Uri(serviceUri);
    }
}
