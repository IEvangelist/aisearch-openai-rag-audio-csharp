namespace Search.OpenAI.RagAudio.WebAPI.Options;

[OptionsValidator]
public sealed partial record class AzureOptions : IValidateOptions<AzureOptions>
{
    [Required]
    public Uri AzureOpenAIEndpoint { get; set; } = new("https://api.openai.com");

    public string AzureOpenAIDeployment { get; set; } = "gpt-4o-realtime-preview-1001";

    [Required]
    public string? AzureOpenAIKey { get; set; }

    [Required]
    public string? AzureOpenAIModel { get; set; }

    public Uri? AzureSearchEndpoint { get; set; }

    public string AzureSearchIndex { get; set; } = "contosobenefits";

    public string? AzureSearchKey { get; set; }
}
