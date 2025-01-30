namespace Search.OpenAI.RagAudio.Web.Extensions;

public static class AIFunctionExtensions
{
    public static ConversationFunctionTool ToConversationFunctionTool(this AIFunction aiFunction)
    {
        return new ConversationFunctionTool()
        {
            Name = aiFunction.Metadata.Name,
            Description = aiFunction.Metadata.Description,
            Parameters = BinaryData.FromString(
                $$"""
                {
                    "type": "object",
                    "properties": {
                        {{string.Join(',', aiFunction.Metadata.Parameters.Select(p => $$"""
                            "{{p.Name}}": {{p.Schema}}
                        """))}}
                    },
                    "required": {{JsonSerializer.Serialize(aiFunction.Metadata.Parameters.Where(p => p.IsRequired).Select(p => p.Name))}}
                }
                """)
        };
    }
}
