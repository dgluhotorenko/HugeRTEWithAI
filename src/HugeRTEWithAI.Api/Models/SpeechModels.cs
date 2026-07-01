namespace HugeRTEWithAI.Api.Models;

/// <summary>Request to synthesize speech from text via ElevenLabs Text-to-Speech.</summary>
public record TextToSpeechRequest
{
    public required string Text { get; init; }

    /// <summary>Optional voice id. Falls back to the configured default when omitted.</summary>
    public string? VoiceId { get; init; }
}

/// <summary>A voice available for Text-to-Speech, exposed to the editor's voice picker.</summary>
public record VoiceInfo
{
    public required string VoiceId { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
}

/// <summary>Result of transcribing an audio recording via ElevenLabs Speech-to-Text (Scribe).</summary>
public record SpeechToTextResponse
{
    public required string Text { get; init; }
    public string? LanguageCode { get; init; }
}