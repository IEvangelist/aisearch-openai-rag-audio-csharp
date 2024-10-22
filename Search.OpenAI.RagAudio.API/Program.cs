var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<RealtimeWebSocketProcessor>();
builder.Services.AddScoped<IToolRegistry, RealtimeWebSocketProcessor>();
builder.Services.AddScoped<ToolFactory>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddOptions<AzureOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
