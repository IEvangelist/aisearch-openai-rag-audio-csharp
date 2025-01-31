namespace Search.OpenAI.RagAudio.Web.Services;

public sealed partial class SearchService(
    SearchClient client,
    IConfiguration configuration,
    ILogger<SearchService> logger)
{
    private AzureSearchConfiguration? _searchConfiguration;

    [Description("""
        Search the knowledge base. The knowledge base is in English, translate to and from English if 
        needed. Results are formatted as a source name first in square brackets, followed by the text 
        content, and a line with '-----' at the end of each result.
        """)]
    public async Task<SearchResult[]> SearchAsync([Description("Search query")] string query)
    {
        logger.LogInformation("Searching for: {Query}", query);

        var config = GetSearchConfiguration();

        var type = Enum.TryParse<SearchQueryType>(config.SemanticConfiguration, ignoreCase: true, out var queryType)
            ? queryType
            : SearchQueryType.Simple;

        var searchOptions = new SearchOptions
        {
            Select = { config.IdentifierField, config.ContentField },
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = config.SemanticConfiguration,
            },
            Size = 5,
            QueryType = type
        };

        if (config.UseVectorQuery)
        {
            var vectorOptions = new VectorSearchOptions();
            vectorOptions.Queries.Add(new VectorizableTextQuery(query)
            {
                Fields = { config.EmbeddingField },
                KNearestNeighborsCount = 50
            });

            searchOptions.VectorSearch = vectorOptions;
        }

        try
        {
            var result = await client.SearchAsync<SearchDocument>(
                searchText: query,
                options: searchOptions);

            List<SearchDocument> documents = [];

            await foreach (var page in result.Value.GetResultsAsync())
            {
                documents.AddRange(page.Document);
            }

            return
            [
                ..documents.Where(d => d.ContainsKey(config.ContentField)
                    && d.ContainsKey(config.IdentifierField))
                    .Select(d => new SearchResult(
                            Title: d[config.TitleField].ToString()!,
                            Chunk: d[config.ContentField].ToString()!,
                    ChunkId: d[config.IdentifierField].ToString()!
                ))
            ];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for: {Query}", query);

            throw;
        }
    }

    [Description("""
        Report use of a source from the knowledge base as part of an answer (effectively, cite the source). Sources 
        appear in square brackets before each knowledge base passage. Always use this tool to cite sources when responding 
        with information from the knowledge base.
        """)]
    public async Task<GroundingData[]> ReportGroundingAsync(
        [Description("List of source names from last statement actually used, do not include the ones not used to formulate a response")]
        string[] sources)
    {
        var config = GetSearchConfiguration();

        string[] keySources = [.. sources.Where(static s => KeyPattern().IsMatch(s))];
        var query = string.Join(" OR ", keySources);

        logger.LogInformation("Reporting groundings: {Query}", query);

        try
        {
            var result = await client.SearchAsync<SearchDocument>(
                searchText: query,
                options: new SearchOptions
                {
                    SearchFields = { config.IdentifierField },
                    Select = { config.IdentifierField, config.TitleField, config.ContentField },
                    Size = keySources.Length,
                    QueryType = SearchQueryType.Full
                });

            List<SearchDocument> documents = [];

            await foreach (var page in result.Value.GetResultsAsync())
            {
                documents.AddRange(page.Document);
            }

            return
            [
                ..documents.Where(d => d.ContainsKey(config.TitleField)
                    && d.ContainsKey(config.ContentField)
                    && d.ContainsKey(config.IdentifierField))
                .Select(d => new GroundingData(Sources: [
                    new SearchResult(
                        Title: d[config.TitleField].ToString()!,
                        Chunk: d[config.ContentField].ToString()!,
                        ChunkId: d[config.IdentifierField].ToString()!
                    )
                ]))
            ];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for: {Query}", query);

            throw;
        }
    }

    private AzureSearchConfiguration GetSearchConfiguration()
    {
        if (_searchConfiguration.HasValue)
        {
            return _searchConfiguration.Value;
        }

        var semanticConfiguration = configuration.GetValue("AZURE_SEARCH_SEMANTIC_CONFIGURATION", "simple");
        var identifierField = configuration.GetValue("AZURE_SEARCH_IDENTIFIER_FIELD", "chunk_id");
        var contentField = configuration.GetValue("AZURE_SEARCH_CONTENT_FIELD", "chunk");
        var embeddingField = configuration.GetValue("AZURE_SEARCH_EMBEDDING_FIELD", "text_vector");
        var titleField = configuration.GetValue("AZURE_SEARCH_TITLE_FIELD", "title");
        var useVectorQuery = configuration.GetValue("AZURE_SEARCH_USE_VECTOR_QUERY", true);

        _searchConfiguration = new AzureSearchConfiguration(
            IdentifierField: identifierField,
            ContentField: contentField,
            EmbeddingField: embeddingField,
            TitleField: titleField,
            SemanticConfiguration: semanticConfiguration,
            UseVectorQuery: useVectorQuery
        );

        logger.LogInformation("Search configuration: {Configuration}", _searchConfiguration);

        return _searchConfiguration.Value;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_=\-]+$")]
    private static partial Regex KeyPattern();
}

internal readonly record struct AzureSearchConfiguration(
    string IdentifierField,
    string ContentField,
    string EmbeddingField,
    string TitleField,
    string SemanticConfiguration,
    bool UseVectorQuery);