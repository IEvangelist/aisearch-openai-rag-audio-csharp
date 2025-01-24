global using System.Buffers;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.IO.Pipelines;
global using System.Net.WebSockets;
global using System.Runtime.InteropServices.JavaScript;
global using System.Text;
global using System.Text.Json;

global using KristofferStrube.Blazor.DOM;
global using KristofferStrube.Blazor.MediaCaptureStreams;
global using KristofferStrube.Blazor.WebAudio;
global using KristofferStrube.Blazor.WebIDL;

global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.JSInterop;

global using Search.OpenAI.RagAudio;
global using Search.OpenAI.RagAudio.Services;
global using Search.OpenAI.RagAudio.Shared;
global using Search.OpenAI.RagAudio.Types;
global using Search.OpenAI.Shared.Messages;
global using Search.OpenAI.Shared.Serialization;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]