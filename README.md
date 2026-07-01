# HugeRTE + AI Assistant & ElevenLabs Voice

A rich text editor with AI-powered writing tools and voice. Built with [HugeRTE](https://hugerte.org/), Azure OpenAI, and [ElevenLabs](https://elevenlabs.io/).

![Demo](docs/HugeRTE_with_AI_assistant.gif)

## Features

### AI writing tools (Azure OpenAI)

The **AI Assistant** toolbar button provides a dropdown with these actions:

| Action | Description |
|--------|-------------|
| **Fix Grammar & Spelling** | Corrects grammar, spelling, and punctuation |
| **Improve Writing** | Enhances clarity, engagement, and structure |
| **Translate to English** | Translates text from any language to English |
| **Expand Text** | Adds more detail, examples, and context |
| **Summarize** | Condenses text to key points |

Select text (or process the entire editor content), pick an action, review the AI suggestion side-by-side with the original, then accept or reject.

### Voice (ElevenLabs)

| Tool | Description |
|------|-------------|
| 🎤 **Dictate** | Records from your microphone and transcribes speech to text ([ElevenLabs Scribe](https://elevenlabs.io/speech-to-text)), inserting it at the cursor |
| 🔊 **Read aloud** | Reads the selection or the whole document out loud ([ElevenLabs Text-to-Speech](https://elevenlabs.io/text-to-speech)), with a voice picker populated from your account |

#### Demo

The clip below is a silent screen capture — watch the Chrome tab indicators: the 🔴 recording dot appears while dictating, and the 🔊 speaker icon appears while "Read aloud" plays the synthesized audio.

https://github.com/user-attachments/assets/30ea67b7-8604-45ee-ac33-7467e1e54523

> **Note on transcription accuracy.** [ElevenLabs Scribe](https://elevenlabs.io/speech-to-text) is highly accurate, but results depend on microphone quality and background noise. Scribe can also tag non-speech audio events (e.g. `(music)`, `(laughter)`); since this app is used for dictation, that tagging is disabled (`tag_audio_events=false`) so the output stays clean text.

## Tech Stack

- **Backend:** .NET 10 Minimal API
- **Frontend:** Vanilla HTML + [HugeRTE](https://hugerte.org/) (via CDN)
- **AI:** Azure OpenAI (GPT-4o-mini)
- **Voice:** ElevenLabs [Text-to-Speech](https://elevenlabs.io/text-to-speech) + [Speech-to-Text](https://elevenlabs.io/speech-to-text) (Scribe)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Microsoft Foundry | Azure OpenAI](https://ai.azure.com/) resource with a deployed model (e.g., `gpt-4o-mini`) — for the AI writing tools
- An [ElevenLabs](https://elevenlabs.io/) account and API key — for the voice features (free tier works)

### 1. Clone the repository

```bash
git clone https://github.com/dgluhotorenko/HugeRTEWithAI.git
cd HugeRTEWithAI
```

### 2. Configure your API keys

Non-secret settings live in `src/HugeRTEWithAI.Api/appsettings.json` (endpoint, deployment name, voice/model defaults). **API keys must not be committed** — store them with [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
cd src/HugeRTEWithAI.Api
dotnet user-secrets init

# Azure OpenAI (AI writing tools)
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-openai-key"
# Optionally override the endpoint / deployment from appsettings.json:
# dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
# dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"

# ElevenLabs (voice features)
dotnet user-secrets set "ElevenLabs:ApiKey" "your-elevenlabs-key"
```

ElevenLabs voice/model defaults can be tuned in `appsettings.json` under `ElevenLabs`:

```json
{
  "ElevenLabs": {
    "ApiKey": "",
    "DefaultVoiceId": "JBFqnCBsd6RMkjVDRZzb",
    "TtsModelId": "eleven_multilingual_v2",
    "SttModelId": "scribe_v1",
    "OutputFormat": "mp3_44100_128"
  }
}
```

> The voice features degrade gracefully: if `ElevenLabs:ApiKey` is not set, the voice endpoints return `503` with a clear message and the rest of the app keeps working.

### 3. Run

```bash
cd src/HugeRTEWithAI.Api
dotnet run
```

Open **http://localhost:5000** in your browser.

## Project Structure

```
src/HugeRTEWithAI.Api/
  Program.cs                          # App startup, DI, API endpoints
  Models/
    TextProcessingModels.cs           # AI text Request/Response DTOs
    SpeechModels.cs                   # TTS/STT/voice DTOs
  Services/
    IAiTextService.cs                 # AI text service interface
    AiTextService.cs                  # Azure OpenAI integration & prompts
    IElevenLabsService.cs             # Voice service interface
    ElevenLabsService.cs              # ElevenLabs TTS/STT/voices integration
    ElevenLabsOptions.cs              # ElevenLabs config binding
  wwwroot/
    index.html                        # Editor page (HugeRTE via CDN)
    plugins/ai-assistant/
      plugin.js                       # HugeRTE plugin (AI tools, dictate, read aloud)
      styles.css                      # Review dialog styling
```

### API endpoints

| Method & path | Purpose |
|---------------|---------|
| `POST /api/process` | AI text action (grammar, improve, translate, expand, summarize) |
| `POST /api/tts` | Text-to-Speech — returns MP3 audio for the given text/voice |
| `POST /api/stt` | Speech-to-Text — transcribes an uploaded audio recording |
| `GET /api/voices` | Lists ElevenLabs voices for the voice picker |

## How It Works

```
User types text in editor
        |
        v
Clicks "AI Assistant" -> selects action (e.g. Improve)
        |
        v
Frontend sends POST /api/process { text, action }
        |
        v
Backend builds prompt -> calls Azure OpenAI
        |
        v
AI response returned -> shown in review dialog
        |
        v
User clicks "Accept" (replaces content) or "Reject" (no changes)
```

## License

MIT
