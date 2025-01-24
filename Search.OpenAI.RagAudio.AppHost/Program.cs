var builder = DistributedApplication.CreateBuilder(args);



var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openai")
            .AddDeployment(deployment: new AzureOpenAIDeployment(
                name: builder.Configuration.GetValue("Azure:AzureOpenAIDeployment", "gpt-4o-realtime-preview-1001"),
                modelName: builder.Configuration.GetValue("Azure:AzureOpenAIModel", "gpt-4o-realtime-preview"),
                modelVersion: builder.Configuration.GetValue("Azure:AzureOpenAIModelVersion", "2024-10-01"))
            )
    : builder.AddConnectionString("openai");

var api = builder.AddProject<Projects.Search_OpenAI_RagAudio_WebAPI>("api")
    .WithReference(openai)
    .WithAzureEnvironmentVariables();

var frontend = builder.AddProject<Projects.Search_OpenAI_RagAudio>("frontend")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
