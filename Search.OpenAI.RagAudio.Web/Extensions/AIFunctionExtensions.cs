﻿namespace Search.OpenAI.RagAudio.Web.Extensions;

public static class AIFunctionExtensions
{
    public static ConversationFunctionTool ToConversationFunctionTool(this AIFunction function)
    {
        var parameters = $$"""
            {
                "type": "object",
                "properties": {
                    {{string.Join(',', function.Metadata.Parameters.Select(p => $$"""
                        "{{p.Name}}": {{p.Schema}}
                    """))}}
                },
                "required": {{JsonSerializer.Serialize(function.Metadata.Parameters.Where(p => p.IsRequired).Select(p => p.Name))}},
                "additionalProperties": false
            }
            """;

        return new ConversationFunctionTool()
        {
            Name = function.Metadata.Name,
            Description = function.Metadata.Description,
            Parameters = BinaryData.FromString(parameters)
        };
    }
}
