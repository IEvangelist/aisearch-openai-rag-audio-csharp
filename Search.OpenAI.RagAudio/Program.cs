var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddLocalStorageServices();
builder.Services.AddMediaDevicesService();

builder.Services.AddScoped<AudioRecorder>();
builder.Services.AddScoped<AudioRecorderService>();
builder.Services.AddScoped<AudioPlayer>();
builder.Services.AddScoped<AudioPlayerService>();

await builder.Build().RunAsync();
