global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics.CodeAnalysis;
global using System.Net.WebSockets;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading.Channels;

global using Azure;
global using Azure.Core;
global using Azure.Identity;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Models;

global using Microsoft.Extensions.Options;

global using Search.OpenAI.RagAudio.API.Search;
global using Search.OpenAI.RagAudio.API.Options;
global using Search.OpenAI.RagAudio.API.Realtime;
global using Search.OpenAI.RagAudio.API.Serialization;
global using Search.OpenAI.RagAudio.API.Services;
