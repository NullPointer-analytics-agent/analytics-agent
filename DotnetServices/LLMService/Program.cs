using Microsoft.SemanticKernel;
using LlmService;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
    //modelId: "gemma3:12b",
    modelId: "qwen2.5:7b",
    apiKey: "none",
    endpoint: new Uri("http://localhost:11434/v1")
);


WebApplication app = builder.Build();

app.MapLLMChatEndpoints();

app.Run();