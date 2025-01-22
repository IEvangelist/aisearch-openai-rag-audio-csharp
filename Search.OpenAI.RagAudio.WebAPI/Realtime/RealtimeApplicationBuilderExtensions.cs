namespace Search.OpenAI.RagAudio.WebAPI.Realtime;

internal static partial class RealtimeApplicationBuilderExtensions
{
    internal static void MapRealtimeEndpoints(this WebApplication app)
    {
        app.Use(RealtimeWebSocketHandler);

        // Simply HTML page to show that the API is ready.
        app.MapGet(pattern: "/", handler: () => Results.Content(
            content: ApiReadyHtml, contentType: "text/html"));
    }

    private static async Task RealtimeWebSocketHandler(HttpContext context, RequestDelegate next)
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
            using var clientWebSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

            if (clientWebSocket is null)
            {
                logger.LogWarning("WebSocket connection wasn't accepted.");
                return;
            }

            var conversationProcessor = services.GetRequiredService<RealtimeConversationProcessor>();

            await conversationProcessor.ProcessAsync(clientWebSocket);
        }
        finally
        {
            await next(context);
        }
    }

    private const string ApiReadyHtml = """
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
                        transition: all 1s ease;
                    }
                    button:hover {
                        background-color: #ff99cc; /* Slightly lighter magenta on hover */
                    }
                    .pulse {
                      height: 100px;
                      width: 200px;
                      overflow: hidden;
                      position: absolute;
                      top: 0;
                      bottom: 0;
                      left: 0;
                      right: 0;
                      margin: auto;
                    }
                    .pulse:after {
                      content: "";
                      display: block;
                      background: url('data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200px 100px"><polyline fill="none" stroke-width="3px" stroke="white" points="2.4,58.7 70.8,58.7 76.1,46.2 81.1,58.7 89.9,58.7 93.8,66.5 102.8,22.7 110.6,78.7 115.3,58.7 126.4,58.7 134.4,54.7 142.4,58.7 197.8,58.7 "/></svg>') 0 0 no-repeat;
                      width: 100%;
                      height: 100%;
                      position: absolute;
                      -webkit-animation: 2s pulse infinite linear;
                              animation: 2s pulse infinite linear;
                    }
                    .pulse:before {
                      content: "";
                      background: #444;
                      position: absolute;
                      z-index: -1;
                      left: 2px;
                      right: 2px;
                      bottom: 0;
                      top: 16px;
                      margin: auto;
                      height: 3px;
                    }

                    @-webkit-keyframes pulse {
                      0% {
                        clip: rect(0, 0, 100px, 0);
                      }
                      10% {
                        clip: rect(0, 66.6px, 100px, 0);
                      }
                      38% {
                        clip: rect(0, 133.3px, 100px, 0);
                      }
                      48% {
                        clip: rect(0, 200px, 100px, 0);
                      }
                      52% {
                        clip: rect(0, 200px, 100px, 0);
                      }
                      62% {
                        clip: rect(0, 200px, 100px, 66.6px);
                      }
                      90% {
                        clip: rect(0, 200px, 100px, 133.3px);
                      }
                      100% {
                        clip: rect(0, 200px, 100px, 200px);
                      }
                    }

                    @keyframes pulse {
                      0% {
                        clip: rect(0, 0, 100px, 0);
                      }
                      10% {
                        clip: rect(0, 66.6px, 100px, 0);
                      }
                      38% {
                        clip: rect(0, 133.3px, 100px, 0);
                      }
                      48% {
                        clip: rect(0, 200px, 100px, 0);
                      }
                      52% {
                        clip: rect(0, 200px, 100px, 0);
                      }
                      62% {
                        clip: rect(0, 200px, 100px, 66.6px);
                      }
                      90% {
                        clip: rect(0, 200px, 100px, 133.3px);
                      }
                      100% {
                        clip: rect(0, 200px, 100px, 200px);
                      }
                    }
                </style>
            </head>
            <body>
                <div>✅ Realtime API is ready!</div>
                <div class="pulse"></div>
                <button onclick="window.close()">❌ Close</button>
            </body>
            </html>
            """;
}
