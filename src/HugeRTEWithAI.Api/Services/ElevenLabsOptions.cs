namespace HugeRTEWithAI.Api.Services;

public class ElevenLabsOptions
{
    public const string SectionName = "ElevenLabs";

    public string ApiKey { get; set; } = "";
    public string DefaultVoiceId { get; set; } = "JBFqnCBsd6RMkjVDRZzb";
    public string TtsModelId { get; set; } = "eleven_multilingual_v2";
    public string SttModelId { get; set; } = "scribe_v1";
    public string OutputFormat { get; set; } = "mp3_44100_128";
}