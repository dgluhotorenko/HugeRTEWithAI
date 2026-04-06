using Azure;
using Azure.AI.OpenAI;
using HugeRTEWithAI.Api.Models;
using HugeRTEWithAI.Api.Services;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Azure OpenAI client
builder.Services.AddSingleton<ChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
    var apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
    var deployment = config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is required.");

    var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    return client.GetChatClient(deployment);
});

builder.Services.AddSingleton<IAiTextService, AiTextService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// POST /api/process
app.MapPost("/api/process", async (TextProcessingRequest request, IAiTextService service, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest(new ErrorResponse { Error = "Text is required." });

    if (string.IsNullOrWhiteSpace(request.Action))
        return Results.BadRequest(new ErrorResponse { Error = "Action is required." });

    if (request.Text.Length > 20_000)
        return Results.BadRequest(new ErrorResponse { Error = "Text is too long. Maximum 20,000 characters." });

    try
    {
        var result = await service.ProcessTextAsync(request);
        return Results.Ok(new TextProcessingResponse { ProcessedText = result });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse { Error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing text");
        return Results.Problem("An error occurred while processing your request.");
    }
});

app.Run();
