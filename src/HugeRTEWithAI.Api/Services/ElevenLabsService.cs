using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HugeRTEWithAI.Api.Models;
using Microsoft.Extensions.Options;

namespace HugeRTEWithAI.Api.Services;

public class ElevenLabsService : IElevenLabsService
{
    private readonly HttpClient _http;
    private readonly ElevenLabsOptions _options;
    private readonly ILogger<ElevenLabsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ElevenLabsService(HttpClient http, IOptions<ElevenLabsOptions> options, ILogger<ElevenLabsService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> TextToSpeechAsync(string text, string? voiceId, CancellationToken cancellationToken)
    {
        EnsureApiKey();

        var voice = string.IsNullOrWhiteSpace(voiceId) ? _options.DefaultVoiceId : voiceId;
        var url = $"v1/text-to-speech/{Uri.EscapeDataString(voice)}?output_format={_options.OutputFormat}";

        var payload = new { text, model_id = _options.TtsModelId };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        _logger.LogInformation("ElevenLabs TTS: voice '{Voice}', {Chars} chars", voice, text.Length);

        using var response = await _http.PostAsync(url, content, cancellationToken);
        await EnsureSuccessAsync(response, "text-to-speech", cancellationToken);

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<SpeechToTextResponse> SpeechToTextAsync(Stream audio, string fileName, string contentType, CancellationToken cancellationToken)
    {
        EnsureApiKey();

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(audio);
        // Browsers send content types like "audio/webm;codecs=opus" — parse (not the
        // strict ctor) so the codec parameter is handled instead of throwing.
        if (!MediaTypeHeaderValue.TryParse(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, out var mediaType))
            mediaType = new MediaTypeHeaderValue("application/octet-stream");
        fileContent.Headers.ContentType = mediaType;
        form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "recording.webm" : fileName);
        form.Add(new StringContent(_options.SttModelId), "model_id");
        // Dictation should produce clean text — don't let Scribe inject non-speech
        // audio-event tags like "(techno music)" or "(laughter)" into the output.
        form.Add(new StringContent("false"), "tag_audio_events");

        _logger.LogInformation("ElevenLabs STT: model '{Model}', file '{File}'", _options.SttModelId, fileName);

        using var response = await _http.PostAsync("v1/speech-to-text", form, cancellationToken);
        await EnsureSuccessAsync(response, "speech-to-text", cancellationToken);

        var dto = await response.Content.ReadFromJsonAsync<SttResponseDto>(JsonOptions, cancellationToken);
        return new SpeechToTextResponse
        {
            Text = dto?.Text ?? "",
            LanguageCode = dto?.LanguageCode,
        };
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken cancellationToken)
    {
        EnsureApiKey();

        using var response = await _http.GetAsync("v1/voices", cancellationToken);

        // The voice picker is a non-critical enhancement. If the key lacks the
        // 'voices_read' permission (or any other error occurs), degrade to an
        // empty list so the editor still works with the configured default voice.
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("ElevenLabs voices unavailable ({Status}): {Body}", (int)response.StatusCode, body);
            return Array.Empty<VoiceInfo>();
        }

        var dto = await response.Content.ReadFromJsonAsync<VoicesResponseDto>(JsonOptions, cancellationToken);
        return dto?.Voices?.Select(v => new VoiceInfo
        {
            VoiceId = v.VoiceId,
            Name = v.Name,
            Category = v.Category,
        }).ToList() ?? new List<VoiceInfo>();
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("ElevenLabs API key is not configured. Set 'ElevenLabs:ApiKey' via user-secrets or environment variables.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("ElevenLabs {Operation} failed ({Status}): {Body}", operation, (int)response.StatusCode, body);
        throw new HttpRequestException($"ElevenLabs {operation} request failed with status {(int)response.StatusCode}.");
    }

    private sealed record SttResponseDto
    {
        [JsonPropertyName("text")] public string? Text { get; init; }
        [JsonPropertyName("language_code")] public string? LanguageCode { get; init; }
    }

    private sealed record VoicesResponseDto
    {
        [JsonPropertyName("voices")] public List<VoiceDto>? Voices { get; init; }
    }

    private sealed record VoiceDto
    {
        [JsonPropertyName("voice_id")] public string VoiceId { get; init; } = "";
        [JsonPropertyName("name")] public string Name { get; init; } = "";
        [JsonPropertyName("category")] public string? Category { get; init; }
    }
}