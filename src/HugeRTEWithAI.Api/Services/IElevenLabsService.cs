using HugeRTEWithAI.Api.Models;

namespace HugeRTEWithAI.Api.Services;

public interface IElevenLabsService
{
    /// <summary>Synthesizes <paramref name="text"/> to speech. Returns the MP3 audio bytes.</summary>
    Task<byte[]> TextToSpeechAsync(string text, string? voiceId, CancellationToken cancellationToken);

    /// <summary>Transcribes an uploaded audio recording to text (ElevenLabs Scribe).</summary>
    Task<SpeechToTextResponse> SpeechToTextAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken);

    /// <summary>Lists the voices available on the account for the voice picker.</summary>
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken);
}