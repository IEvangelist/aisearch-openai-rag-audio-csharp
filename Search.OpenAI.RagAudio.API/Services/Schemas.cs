namespace Search.OpenAI.RagAudio.API.Services;

internal static class Schemas
{
    internal static readonly object SearchTookSchema = new
    {
        type = "function",
        name = "search",
        description = "Search the knowledge base. The knowledge base is in English, translate to and from English if " +
                      "needed. Results are formatted as a source name first in square brackets, followed by the text " +
                      "content, and a line with '-----' at the end of each result.",
        parameters = new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string", description = "Search query" }
            },
            required = new string[] { "query" }
        }
    };

    internal static readonly object GroundingToolSchema = new
    {
        type = "function",
        name = "report_grounding",
        description = "Report use of a source from the knowledge base as part of an answer (effectively, cite the source). Sources " +
                      "appear in square brackets before each knowledge base passage. Always use this tool to cite sources when responding " +
                      "with information from the knowledge base.",
        parameters = new
        {
            type = "object",
            properties = new
            {
                sources = new
                {
                    type = "array",
                    items = new { type = "string" },
                    description = "List of source names from last statement actually used, do not include the ones not used to formulate a response"
                }
            },
            required = new string[] { "sources" }
        }
    };
}
