# HugeRTE + AI Assistant

A rich text editor with AI-powered writing tools. Built with [HugeRTE](https://hugerte.org/) and Azure OpenAI.

![Demo](docs/HugeRTE_with_AI_assistant.gif)

## Features

The AI Assistant toolbar button provides a dropdown with these actions:

| Action | Description |
|--------|-------------|
| **Fix Grammar & Spelling** | Corrects grammar, spelling, and punctuation |
| **Improve Writing** | Enhances clarity, engagement, and structure |
| **Translate to English** | Translates text from any language to English |
| **Expand Text** | Adds more detail, examples, and context |
| **Summarize** | Condenses text to key points |

Select text (or process the entire editor content), pick an action, review the AI suggestion side-by-side with the original, then accept or reject.

## Tech Stack

- **Backend:** .NET 10 Minimal API
- **Frontend:** Vanilla HTML + [HugeRTE](https://hugerte.org/) (via CDN)
- **AI:** Azure OpenAI (GPT-4o-mini)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Microsoft Foundry | Azure OpenAI](https://ai.azure.com/) resource with a deployed model (e.g., `gpt-4o-mini`)

### 1. Clone the repository

```bash
git clone https://github.com/dgluhotorenko/HugeRTEWithAI.git
cd HugeRTEWithAI
```

### 2. Configure your API key

Edit `src/HugeRTEWithAI.Api/appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

> Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep keys out of source control:
> ```bash
> cd src/HugeRTEWithAI.Api
> dotnet user-secrets init
> dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key-here"
> dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
> dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
> ```

### 3. Run

```bash
cd src/HugeRTEWithAI.Api
dotnet run
```

Open **http://localhost:5000** in your browser.

## Project Structure

```
src/HugeRTEWithAI.Api/
  Program.cs                          # App startup, DI, API endpoint
  Models/TextProcessingModels.cs      # Request/Response DTOs
  Services/
    IAiTextService.cs                 # Service interface
    AiTextService.cs                  # Azure OpenAI integration & prompts
  wwwroot/
    index.html                        # Editor page (HugeRTE via CDN)
    plugins/ai-assistant/
      plugin.js                       # HugeRTE plugin (toolbar, dialog)
      styles.css                      # Review dialog styling
```

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
