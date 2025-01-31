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
            var indexClient = provider.GetRequiredService<SearchIndexClient>();

            return indexClient.GetSearchClient(options.AzureSearchIndex);
        });

        services.AddLocalStorageServices();
        services.AddScoped<SearchService>();
        services.AddScoped<RealtimeToolFactory>();
        services.AddScoped<MicrophoneSignal>();
        services.AddScoped<AppJSModule>();

        return services;
    }
}
