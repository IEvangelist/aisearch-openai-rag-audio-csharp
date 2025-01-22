namespace Search.OpenAI.RagAudio.WebAPI.Realtime;

internal static class RealtimeServiceCollectionExtensions
{
    internal static IServiceCollection AddRealtimeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<RealtimeConversationProcessor>();

        services.AddOptions<AzureOptions>()
                .Bind(configuration.GetSection("Azure"))
                .ValidateDataAnnotations();

        return services;
    }
}
