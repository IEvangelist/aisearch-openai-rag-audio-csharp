namespace Search.OpenAI.RagAudio.Web.Realtime;

public sealed class RealtimeToolFactory(SearchService searchService, IOptions<AzureOptions> options)
{
    public List<AIFunction> CreateRealtimeTools()
    {
        if (options is { Value: { AzureSearchEndpoint: null, AzureSearchKey: null } })
        {
            return [];
        }

        var search = AIFunctionFactory.Create(searchService.SearchAsync);
        var reportGrounding = AIFunctionFactory.Create(searchService.ReportGroundingAsync);

        return
        [
            search,
            reportGrounding
        ];
    }
}
