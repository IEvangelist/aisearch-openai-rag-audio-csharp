namespace Aspire.Hosting;

internal static class AppEvents
{
    internal static Func<BeforeResourceStartedEvent, CancellationToken, Task> BeforeFrontendStarted =
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
                .UriString;

            logger.LogInformation("Starting frontend: {Url}", url);
        }

        return Task.CompletedTask;
    };
}
