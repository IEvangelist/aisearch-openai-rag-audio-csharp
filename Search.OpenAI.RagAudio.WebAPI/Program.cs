var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddRealtimeServices(builder.Configuration);

builder.Services.AddScoped(provider =>
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

//builder.AddAzureOpenAIClient("openai", settings =>
//{
//    settings.DisableMetrics = false;
//    settings.DisableTracing = false;

//    if (builder.Configuration["Azure:AzureOpenAIEndpoint"] is { } endpoint)
//    {
//        settings.Endpoint = new(endpoint);
//    }

//    if (builder.Configuration["Azure:AzureOpenAIKey"] is { } key)
//    {
//        settings.Key = key;
//        settings.Credential = new AzureKeyCredential(key);
//    }
//});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseWebSockets();
app.MapRealtimeEndpoints();

app.Run();
