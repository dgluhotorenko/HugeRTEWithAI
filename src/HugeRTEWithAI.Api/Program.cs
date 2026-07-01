using Azure;
using Azure.AI.OpenAI;
using HugeRTEWithAI.Api.Models;
using HugeRTEWithAI.Api.Services;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// ElevenLabs (Text-to-Speech + Speech-to-Text)
builder.Services.Configure<ElevenLabsOptions>(builder.Configuration.GetSection(ElevenLabsOptions.SectionName));
builder.Services.AddHttpClient<IElevenLabsService, ElevenLabsService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElevenLabsOptions>>().Value;
    client.BaseAddress = new Uri("https://api.elevenlabs.io/");
    client.Timeout = TimeSpan.FromMinutes(2);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
        client.DefaultRequestHeaders.Add("xi-api-key", options.ApiKey);
});

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

// GET /api/voices — list voices for the editor's voice picker
app.MapGet("/api/voices", async (IElevenLabsService service, ILogger<Program> logger, CancellationToken ct) =>
{
    try
    {
        var voices = await service.GetVoicesAsync(ct);
        return Results.Ok(voices);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching voices");
        return Results.Problem("An error occurred while fetching voices.");
    }
});

// POST /api/tts — synthesize speech, returns MP3 audio
app.MapPost("/api/tts", async (TextToSpeechRequest request, IElevenLabsService service, ILogger<Program> logger, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest(new ErrorResponse { Error = "Text is required." });

    if (request.Text.Length > 5_000)
        return Results.BadRequest(new ErrorResponse { Error = "Text is too long. Maximum 5,000 characters." });

    try
    {
        var audio = await service.TextToSpeechAsync(request.Text, request.VoiceId, ct);
        return Results.File(audio, "audio/mpeg");
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error synthesizing speech");
        return Results.Problem("An error occurred while synthesizing speech.");
    }
});

// POST /api/stt — transcribe an uploaded audio recording to text
app.MapPost("/api/stt", async (IFormFile file, IElevenLabsService service, ILogger<Program> logger, CancellationToken ct) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new ErrorResponse { Error = "Audio file is required." });

    try
    {
        await using var stream = file.OpenReadStream();
        var result = await service.SpeechToTextAsync(stream, file.FileName, file.ContentType, ct);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error transcribing audio");
        return Results.Problem("An error occurred while transcribing audio.");
    }
}).DisableAntiforgery();

app.Run();
