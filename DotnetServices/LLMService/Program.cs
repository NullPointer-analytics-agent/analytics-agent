using Microsoft.SemanticKernel;
using LLMChatService;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
    modelId: "qwen2.5:7b",
    apiKey: "none",
    endpoint: new Uri("http://localhost:11434/v1")
);


WebApplication app = builder.Build();

app.MapLLMChatEndpoints();

app.Run();