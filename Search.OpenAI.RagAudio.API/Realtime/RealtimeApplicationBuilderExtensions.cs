namespace Microsoft.AspNetCore.Builder;

internal static partial class RealtimeApplicationBuilderExtensions
{
    internal static void MapRealtimeEndpoint(this WebApplication app)
    {
        app.Use(static async (context, next) =>
        {
            try
            {
                if (context.Request.Path != "/realtime")
                {
                    return;
                }

                if (context.WebSockets.IsWebSocketRequest is false)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await using var serviceScope = context.RequestServices.CreateAsyncScope();
                var services = serviceScope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                using var scope = logger.BeginScope("Processing incoming WebSocket request to the server.");
                using var serverWebSocket = await context.WebSockets.AcceptWebSocketAsync();

                if (serverWebSocket is null)
                {
                    logger.LogWarning("WebSocket connection wasn't accepted.");

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

                await processor.ProcessAsync(context, serverWebSocket);
            }
            finally
            {
                await next(context);
            }
        });

        app.MapGet("/", static () => Results.Content(content: """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>API Ready</title>
                <style>
                    * {
                        font-family: monospace;
                        font-size: calc(20px + 5vmin);
                        font-weight: bold;
                    }

                    body {
                        margin: 0;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        flex-direction: column;
                        height: 100vh;
                        background-color: #222; /* Darker background */
                        color: #00cccc; /* Less intense cyan */                        
                    }
                    button {
                        margin-top: 4rem;
                        padding: 1rem 1.5rem;
                        background-color: #8B008B; /* Really dark magenta */
                        color: #222; /* Dark text color */
                        border: none;
                        border-radius: 2rem;
                        cursor: pointer;
                    }
                    button:hover {
                        background-color: #ff99cc; /* Slightly lighter magenta on hover */
                    }
                </style>
            </head>
            <body>
                <div>✅ Realtime API is ready!</div>
                <button onclick="window.close()">❌ Close</button>
            </body>
            </html>
            """, contentType: "text/html"));
    }
}
