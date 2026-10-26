using Microsoft.SemanticKernel;
using LlmService;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddKernel();

builder.Services.AddSingleton<PromptParameters>();
builder.Services.AddSingleton<IPromptHandler, PromptHandler>();

builder.Services.AddOpenAIChatCompletion(

    modelId: "qwen2.5:7b",
    apiKey: "none",
    endpoint: new Uri("http://localhost:11434/v1"),
    serviceId: "qwen"
);

builder.Services.AddOpenAIChatCompletion(
    modelId: "gemma3:12b",
    apiKey: "none",
    endpoint: new Uri("http://localhost:11434/v1"),
    serviceId: "gemma"
);

builder.Services.AddSingleton<PromptParameters>();


WebApplication app = builder.Build();

app.MapLLMChatEndpoints();

app.Run();