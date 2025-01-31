namespace Search.OpenAI.RagAudio.Web.Extensions;

public static class ConversationItemStreamingFinishedExtensions
{
    public static async Task<ConversationItem?> GetFunctionCallOutputAsync<T>(
        this ConversationItemStreamingFinishedUpdate update,
        ISet<AIFunction> functions,
        ILogger<T> logger,
        CancellationToken cancellationToken)
    {
        if (update.FunctionCallOutput is { Length: > 0 } callOutput)
        {
            logger.LogInformation("Function call output exists: {Output}", callOutput);
        }

        if (string.IsNullOrEmpty(update.FunctionName))
        {
            logger.LogInformation("Function name is null or empty");

            return null;
        }

        if (functions.FirstOrDefault(t => t.Metadata.Name == update.FunctionName) is not AIFunction function)
        {
            logger.LogInformation("There are no matching function names for: {Func}", update.FunctionName);

            return null;
        }

        try
        {
            var jsonArgs = JsonSerializer.Deserialize<Dictionary<string, object?>>(update.FunctionCallArguments);

            var output = await function.InvokeAsync(jsonArgs, cancellationToken);

            var item  = ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, output?.ToString() ?? "");

            if (item is not null)
            {
                logger.LogInformation("Successfully invoked {Func} tool.", update.FunctionName);
            }

            return item;
        }
        catch (JsonException ex)
        {
            logger.LogError("Error parsing JSON for {Func}: {Ex}", update.FunctionName, ex);

            return ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, "Invalid JSON");
        }
        catch
        {
            logger.LogError("Error calling {Func} tool.", update.FunctionName);

            return ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, "Error calling tool");
        }
    }
}
