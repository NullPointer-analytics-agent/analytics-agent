using Microsoft.SemanticKernel;
using LlmService;
using LlmService.Exceptions;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<PromptParameters>();
builder.Services.AddSingleton<IPromptHandler, PromptHandler>();
builder.Services.AddTransient<ExceptionMiddleware>();

var maiApiUrl = builder.Configuration["LlmSettings:MaiApiUrl"] ?? "https://api-ai.mai.ru/v1";
var maiApiKey = builder.Configuration["LlmSettings:MaiApiKey"] ?? "none";
var maiModelName = builder.Configuration["LlmSettings:MaiModelName"] ?? "deepseek-v4.1-flash";

Console.WriteLine($"[DEBUG] URL={maiApiUrl}, Model={maiModelName}, KeyLen={maiApiKey.Length}");

HttpClientHandler handler = new()
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

HttpClient httpClient = new(handler)
{
    BaseAddress = new Uri(maiApiUrl.TrimEnd('/') + "/"),
    Timeout = TimeSpan.FromMinutes(5)
};

builder.Services.AddKernel().AddOpenAIChatCompletion(
    modelId: maiModelName,
    apiKey: maiApiKey,
    httpClient: httpClient
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddSingleton<PromptParameters>();


WebApplication app = builder.Build();

app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.MapLLMChatEndpoints();

app.Run();