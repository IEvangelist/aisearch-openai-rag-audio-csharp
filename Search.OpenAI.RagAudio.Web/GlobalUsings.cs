global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.IO.Pipelines;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;

global using Azure;
global using Azure.AI.OpenAI;
global using Azure.Search.Documents;
global using Azure.Search.Documents.Indexes;
global using Azure.Search.Documents.Models;

global using Microsoft.Extensions.AI;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.Extensions.Options;
global using Microsoft.JSInterop;

global using OpenAI.RealtimeConversation;

global using Search.OpenAI.RagAudio.Web.Components;
global using Search.OpenAI.RagAudio.Web.Components.Shared;
global using Search.OpenAI.RagAudio.Web.Extensions;
global using Search.OpenAI.RagAudio.Web.Models;
global using Search.OpenAI.RagAudio.Web.Options;
global using Search.OpenAI.RagAudio.Web.Realtime;
global using Search.OpenAI.RagAudio.Web.Serialization;
global using Search.OpenAI.RagAudio.Web.Services;

global using static Search.OpenAI.RagAudio.Web.Components.Shared.Dialog;
global using DialogOptions = Search.OpenAI.RagAudio.Web.Components.Shared.Dialog.DialogOptions;
