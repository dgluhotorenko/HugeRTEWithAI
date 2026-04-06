namespace HugeRTEWithAI.Api.Models;

public record TextProcessingRequest
{
    public required string Text { get; init; }
    public required string Action { get; init; }
    public string? CustomPrompt { get; init; }
}

public record TextProcessingResponse
{
    public required string ProcessedText { get; init; }
}

public record ErrorResponse
{
    public required string Error { get; init; }
}
