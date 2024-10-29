var builder = DistributedApplication.CreateBuilder(args);

var api =
    builder.AddProject<Projects.Search_OpenAI_RagAudio_API>("api");

var frontend =
    builder.AddProject<Projects.Search_OpenAI_RagAudio>("frontend")
           .WithReference(api)
           .WaitFor(api);

builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
    frontend.Resource,
    static (@event, cancellationToken) =>
    {
        var logger = @event.Services.GetRequiredService<ILogger<Program>>();

        if (logger.IsEnabled(LogLevel.Information) &&
            cancellationToken.IsCancellationRequested is false)
        {
            var url = @event.Resource.Annotations.Where(
                    predicate: static annotation => annotation is AllocatedEndpoint)
                .Cast<AllocatedEndpoint>()
                .FirstOrDefault()?
                .Address;

            logger.LogInformation("Starting frontend: 🔗 {Url}", url);
        }

        return Task.CompletedTask;
    });

builder.Build().Run();
