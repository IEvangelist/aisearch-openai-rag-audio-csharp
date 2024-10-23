var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<RealtimeWebSocketProcessor>();
builder.Services.AddScoped<IToolRegistry, RealtimeWebSocketProcessor>();
builder.Services.AddScoped<ToolFactory>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddOptions<AzureOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseWebSockets(new()
{
    AllowedOrigins =
    {
        "https://localhost:56882",
        "http://localhost:56883"
    },
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.MapRealtimeEndpoint();

app.Run();
