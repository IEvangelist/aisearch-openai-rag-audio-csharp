using System.ComponentModel;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;

namespace Search.OpenAI.RagAudio.WebAPI.Search;

internal sealed partial class SearchService
{
    private readonly AzureOptions _azureOptions;
    private readonly ILogger<SearchService> _logger;
    private readonly SearchClient _searchClient;

    // public ConversationTool[] GetTools()
    // {
    //     return 
    //     [
    //         AIFunctionFactory.Create(SearchAsync, new AIFunctionFactoryCreateOptions
    //         {
    //             Name = "search"
    //         })
    //     ];
    // }

    public SearchService(
        IOptions<AzureOptions> options,
        ILogger<SearchService> logger)
    {
        _azureOptions = options.Value;
        _logger = logger;

        _searchClient = _azureOptions.AzureSearchKey is { } searchKey
            ? new SearchClient(
                    _azureOptions.AzureSearchEndpoint,
                    _azureOptions.AzureSearchIndex,
                    new AzureKeyCredential(searchKey))
            : new SearchClient(
                    _azureOptions.AzureSearchEndpoint,
                    _azureOptions.AzureSearchIndex,
                    new DefaultAzureCredential());
    }

    [Description("Search the knowledge base. The knowledge base is in English, translate to and from English if " +
                 "needed. Results are formatted as a source name first in square brackets, followed by the text " +
                 "content, and a line with '-----' at the end of each result.")]
    public async Task<string?> SearchAsync(string? args)
    {
        if (args is null)
        {
            return null;
        }

        var searchArgs = JsonSerializer.Deserialize(
            args, SerializationContext.Default.SearchArgs)!;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Search for '{Query}' in the knowledge base.", searchArgs.Query);
        }

        var response = await _searchClient.SearchAsync<SearchResult>(
            searchArgs.Query,
                    new SearchOptions
                    {
                        QueryType = SearchQueryType.Semantic,
                        Size = 5,
                        VectorSearch = new VectorSearchOptions
                        {
                            Queries =
                            {
                                new VectorizableTextQuery(searchArgs.Query)
                                {
                                    KNearestNeighborsCount = 50, Fields = { "text_vector" }
                                }
                            }
                        },
                        Select = { "chunk_id", "title", "chunk" }
                    });

        StringBuilder buffer = new();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            buffer.AppendLine($"[{result.Document.ChunkId}]: {result.Document.Chunk}")
                  .AppendLine("-----");
        }

        return buffer.ToString();
    }

    [Description("Report use of a source from the knowledge base as part of an answer (effectively, cite the source). Sources " +
                 "appear in square brackets before each knowledge base passage. Always use this tool to cite sources when responding " +
                 "with information from the knowledge base.")]
    public async Task<string?> ReportGroundingAsync(string? args)
    {
        if (args is null)
        {
            return null;
        }

        var sources = JsonSerializer.Deserialize(args, SerializationContext.Default.GroundingArgs)!
            .Sources
            .Where(s => ValidateGroundingSourceFormat().IsMatch(s))
            .ToArray(); // Eagerly materialize because we need the count

        if (sources.Length is 0)
        {
            return null;
        }

        var condition = string.Join(" OR ", sources);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Grounding source: {Condition}", condition);
        }

        // Use search instead of filter to align with how default integrated vectorization indexes
        // are generated, where chunk_id is searchable with a keyword tokenizer, not filterable 
        var results = await _searchClient.SearchAsync<SearchResult>(
            condition,
            new SearchOptions
            {
                QueryType = SearchQueryType.Full,
                Size = sources.Length,
                Select = { "chunk_id", "title", "chunk" },
                SearchFields = { "chunk_id" }
            });

        var documents = new List<SearchResult>();

        await foreach (var result in results.Value.GetResultsAsync())
        {
            documents.Add(result.Document);
        }

        var groundingData = new GroundingData([ ..documents ]);

        return JsonSerializer.Serialize(
            groundingData, SerializationContext.Default.GroundingData);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_=\-]+$")]
    private static partial Regex ValidateGroundingSourceFormat();
}
