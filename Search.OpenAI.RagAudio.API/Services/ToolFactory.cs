namespace Search.OpenAI.RagAudio.API.Services;

internal sealed class ToolFactory(ILogger<ToolFactory> logger)
{
    internal Tool Create(
        string name,
        object schema,
        ToolResultDestination destination,
        Func<string?, Task<string?>> target)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("""
                Creating {Dest} tool: {Name}
                  Schema: {Schema}
                """,
                destination, name, schema);
        }

        return new Tool(
            Name: name,
            Schema: schema,
            Destination: destination,
            Target: target);
    }
}
