using HugeRTEWithAI.Api.Models;

namespace HugeRTEWithAI.Api.Services;

public interface IAiTextService
{
    Task<string> ProcessTextAsync(TextProcessingRequest request);
}
