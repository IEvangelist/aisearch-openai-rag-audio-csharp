namespace Search.OpenAI.RagAudio.Web.Realtime;

public sealed class RealtimeToolFactory(SearchService searchService)
{
    public List<ConversationFunctionTool> CreateRealtimeTools()
    {
        var search = AIFunctionFactory.Create(searchService.SearchAsync);
        var reportGrounding = AIFunctionFactory.Create(searchService.ReportGroundingAsync);

        return
        [
            search.ToConversationFunctionTool(),
            reportGrounding.ToConversationFunctionTool()
        ];
    }
}
