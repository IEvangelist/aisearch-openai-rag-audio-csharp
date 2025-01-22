namespace Aspire.Hosting;

internal static class ResourceBuilderExtensions
{
    internal static IResourceBuilder<T> WithAzureEnvironmentVariables<T>(
        this IResourceBuilder<T> builder)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            var azureSection = builder.ApplicationBuilder.Configuration.GetSection("Azure");

            foreach (var (key, value) in azureSection.GetChildren().SelectMany(static c => c.AsEnumerable()))
            {
                if (value is null)
                {
                    continue;
                }

                var envVarName = key.Replace(":", "__");

                context.EnvironmentVariables[envVarName] = value;
            }
        });
    }
}
