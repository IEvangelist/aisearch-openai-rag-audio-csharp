var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
            .AddDeployment(deployment: new AzureOpenAIDeployment(
                name: builder.Configuration.GetValue("Azure:AzureOpenAIDeployment", "gpt-4o-realtime-preview-1001"),
                modelName: builder.Configuration.GetValue("Azure:AzureOpenAIModel", "gpt-4o-realtime-preview"),
                modelVersion: builder.Configuration.GetValue("Azure:AzureOpenAIModelVersion", "2024-10-01"))
            )
    : builder.AddConnectionString("openai");

builder.AddProject<Projects.Search_OpenAI_RagAudio_Web>("blazor-server-side")
    .WithReference(openai)
    .WithAzureEnvironmentVariables();

builder.Build().Run();
