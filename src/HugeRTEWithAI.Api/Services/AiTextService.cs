using Azure.AI.OpenAI;
using HugeRTEWithAI.Api.Models;
using OpenAI.Chat;

namespace HugeRTEWithAI.Api.Services;

public class AiTextService : IAiTextService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<AiTextService> _logger;

    private static readonly Dictionary<string, string> Prompts = new()
    {
        ["grammar"] = "Fix the grammar, spelling, and punctuation in the following text. Preserve the original meaning, tone, and HTML formatting. Return only the corrected text.",
        ["improve"] = "Improve the following text by making it clearer, more engaging, and better structured. Preserve the original meaning and HTML formatting. Return only the improved text.",
        ["translate"] = "Translate the following text to English. If it is already in English, improve it. Preserve HTML formatting. Return only the translated text.",
        ["expand"] = "Expand the following text by adding more detail, examples, and context. Preserve the original tone and HTML formatting. Return only the expanded text.",
        ["summarize"] = "Summarize the following text concisely, capturing the key points. Preserve HTML formatting. Return only the summary.",
    };

    private static readonly HashSet<string> SupportedActions = new(Prompts.Keys);

    public AiTextService(ChatClient chatClient, ILogger<AiTextService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> ProcessTextAsync(TextProcessingRequest request)
    {
        if (!SupportedActions.Contains(request.Action) && string.IsNullOrWhiteSpace(request.CustomPrompt))
            throw new ArgumentException($"Unsupported action: '{request.Action}'. Supported: {string.Join(", ", SupportedActions)}");

        var prompt = BuildPrompt(request);

        _logger.LogInformation("Processing action '{Action}'", request.Action);

        // NOTE: If your Azure OpenAI deployment already has a system prompt configured,
        // you can remove the SystemChatMessage below to avoid duplication.
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are an AI writing assistant embedded in a rich text editor. Your job is to help users improve their writing. Always return only the processed text — no explanations, no preamble, no markdown code fences. Preserve HTML formatting."),
            new UserChatMessage(prompt),
        };

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 4096,
            Temperature = 0.3f,
        };

        var response = await _chatClient.CompleteChatAsync(messages, options);
        var result = response.Value.Content[0].Text?.Trim();

        if (string.IsNullOrEmpty(result))
        {
            throw new InvalidOperationException("AI returned an empty response.");
        }

        return result;
    }

    private static string BuildPrompt(TextProcessingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CustomPrompt))
        {
            return $"{request.CustomPrompt}\n\nText:\n{request.Text}";
        }

        var template = Prompts[request.Action];
        return $"{template}\n\nText:\n{request.Text}";
    }
}
