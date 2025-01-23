global using System.ClientModel.Primitives;
global using System.ComponentModel.DataAnnotations;
global using System.Net.WebSockets;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization.Metadata;
global using System.Text.RegularExpressions;

global using Azure;
global using Azure.AI.OpenAI;

global using Microsoft.Extensions.Options;

global using OpenAI.RealtimeConversation;

global using Search.OpenAI.RagAudio.WebAPI.Options;
global using Search.OpenAI.RagAudio.WebAPI.Realtime;
global using Search.OpenAI.Shared.Search;
global using Search.OpenAI.Shared.Messages;
global using Search.OpenAI.Shared.Serialization;