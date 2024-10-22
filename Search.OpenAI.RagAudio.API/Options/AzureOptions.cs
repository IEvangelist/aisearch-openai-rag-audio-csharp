namespace Search.OpenAI.RagAudio.API.Options;

[OptionsValidator]
public sealed partial record class AzureOptions : IValidateOptions<AzureOptions>
{
    [Required, Url]
    public Uri AzureOpenAIEndpoint { get; set; } = new("https://api.openai.com");

    [Required, MinLength(3)]
    public string AzureOpenAIDeployment { get; set; } = "gpt-4o-realtime-preview";

    public string? AzureOpenAIKey { get; set; }

    [Required, Url]
    public Uri? AzureSearchEndpoint { get; set; }

    [Required, MinLength(3)]
    public string AzureSearchIndex { get; set; } = "contosobenefits";

    public string? AzureSearchKey { get; set; }
}
