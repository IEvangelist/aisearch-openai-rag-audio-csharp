namespace Search.OpenAI.RagAudio.Web.Realtime;

internal static class RealtimeServiceCollectionExtensions
{
    internal static IServiceCollection AddRealtimeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RealtimeConversationProcessor>();

        services.AddOptions<AzureOptions>()
                .Bind(configuration.GetSection("Azure"))
                .ValidateDataAnnotations();

        services.AddScoped(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureOptions>>().Value;

            if (options.AzureOpenAIEndpoint is not { } endpoint)
            {
                throw new InvalidOperationException(
                    "An Azure:AzureOpenAIEndpoint value is required.");
            }

            if (options.AzureOpenAIKey is not { } key)
            {
                throw new InvalidOperationException(
                    "An Azure:AzureOpenAIKey value is required.");
            }

            return new AzureOpenAIClient(
                endpoint, new AzureKeyCredential(key));
        });

        services.AddLocalStorageServices();
        services.AddScoped<MicrophoneSignal>();

        return services;
    }
}
