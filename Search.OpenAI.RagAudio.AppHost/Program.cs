var builder = DistributedApplication.CreateBuilder(args);

var api =
    builder.AddProject<Projects.Search_OpenAI_RagAudio_API>("api");

var frontend =
    builder.AddProject<Projects.Search_OpenAI_RagAudio>("frontend")
           .WithReference(api);

builder.Build().Run();
