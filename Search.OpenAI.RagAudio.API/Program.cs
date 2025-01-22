var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<RealtimeWebSocketProcessor>();
builder.Services.AddScoped<IToolRegistry, RealtimeWebSocketProcessor>();
builder.Services.AddScoped<ToolFactory>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddOptions<AzureOptions>()
    .Configure(options =>
    {
        var config = builder.Configuration;

        options.AzureOpenAIDeployment = config.GetValue("AZURE_OPENAI_REALTIME_DEPLOYMENT", "");
        options.AzureOpenAIEndpoint = config.GetValue<Uri>("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("""
                An Azure OpenAI Endpoint is essential to this API functioning correctly.
                Please configure an AZURE_OPENAI_ENDPOINT env var.
                """);

        options.AzureSearchEndpoint = config.GetValue<Uri>("AZURE_SEARCH_ENDPOINT")
            ?? throw new InvalidOperationException("""
                An Azure Search Endpoint is essential to this API functioning correctly.
                Please configure an AZURE_SEARCH_ENDPOINT env var.
                """);
        options.AzureSearchIndex = config.GetValue("AZURE_SEARCH_INDEX", "");
    })
    .ValidateDataAnnotations();    

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

var urlsString = builder.Configuration.GetValue<string>("ASPNETCORE_URLS");
var urls = urlsString?.Split(';') ?? [];
IList<string> allowedOrigins =
[
    ..urls,
    "https://localhost:56882",
    "http://localhost:56883"
];
var options = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
foreach (var origin in allowedOrigins)
{
    options.AllowedOrigins.Add(origin);
}

app.UseWebSockets(options);
app.MapRealtimeEndpoint();

app.Run();
