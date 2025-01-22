var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddInMemoryCollection(
    [
        new KeyValuePair<string, string?>(
            "ApiAddress", Environment.GetEnvironmentVariable("services__api__https__0"))
    ]);

builder.Services.AddLocalStorageServices();
builder.Services.AddMediaDevicesService();

builder.Services.AddScoped<AudioRecorder>();
builder.Services.AddScoped<AudioRecorderService>();
builder.Services.AddScoped<AudioPlayer>();
builder.Services.AddScoped<AudioPlayerService>();
builder.Services.AddScoped<WebSocketService>();

await JSHost.ImportAsync(
    moduleName: nameof(DarkModeJSModule),
    moduleUrl: $"../js/dark-mode-toggle.js?{Guid.NewGuid()}" /* cache bust */);

await builder.Build().RunAsync();
