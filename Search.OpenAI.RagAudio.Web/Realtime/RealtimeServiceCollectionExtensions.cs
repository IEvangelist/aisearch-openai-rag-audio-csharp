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

        services.AddScoped(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AzureOptions>>().Value;

            //if (options.AzureSearchEndpoint is not { } endpoint)
            //{
            //    throw new InvalidOperationException(
            //        "An Azure:AzureSearchEndpoint value is required.");
            //}

            //if (options.AzureSearchKey is not { } key)
            //{
            //    throw new InvalidOperationException(
            //        "An Azure:AzureSearchKey value is required.");
            //}

            //var indexClient = new SearchIndexClient(
            //    endpoint, new AzureKeyCredential(key));

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
