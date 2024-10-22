namespace Microsoft.AspNetCore.Builder;

internal static class RealtimeApplicationBuilderExtensions
{
    internal static void MapRealtimeEndpoint(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                if (context.Request.Path != "/realtime")
                {
                    return;
                }

                await using var serviceScope = context.RequestServices.CreateAsyncScope();
                var services = serviceScope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                if (context.WebSockets.IsWebSocketRequest is false)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }

                using var scope = logger.BeginScope("Processing WebSocket request.");
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                if (webSocket is null)
                {
                    return;
                }

                var realtimeProcessor = services.GetRequiredService<RealtimeWebSocketProcessor>();
                var azureOptions = services.GetRequiredService<IOptions<AzureOptions>>().Value;

                // Determine authentication method based on Azure key configuration.
                var processor = (azureOptions.AzureOpenAIKey, azureOptions.AzureSearchKey) switch
                {
                    ({ } openAIKey, null) => realtimeProcessor.WithAzureKeyCredential(openAIKey),
                    (null, { } searchKey) => realtimeProcessor.WithAzureKeyCredential(searchKey),
                    _ => realtimeProcessor.WithTokenCredential(new DefaultAzureCredential())
                };

                await processor.ProcessAsync(context, webSocket);
            }
            finally
            {
                await next(context);
            }
        });
    }
}
