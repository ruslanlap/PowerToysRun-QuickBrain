# QuickBrain Settings Guide

QuickBrain stores settings in `settings.json` file located in the plugin directory.

To edit settings:
1. Right-click on any QuickBrain result
2. Select "Open Settings" from the context menu
3. Edit the JSON file
4. Save and reload PowerToys Run

## Available Settings

### General Settings

- **MaxResults** (1-20, default: 5)  
  Maximum number of results to display

- **Precision** (0-28, default: 10)  
  Number of decimal places for calculation results

- **AngleUnit** (Radians/Degrees/Gradians, default: Radians)  
  Default unit for trigonometric functions
  - `0` = Radians
  - `1` = Degrees
  - `2` = Gradians

- **Theme** (Auto/Light/Dark, default: "Auto")  
  Icon theme (currently not used)

### History Settings

- **EnableHistory** (true/false, default: true)  
  Save calculation history

- **HistoryLimit** (0-1000, default: 100)  
  Maximum number of history entries to keep

### Natural Language Settings

- **EnableNaturalLanguage** (true/false, default: false)  
  Process natural language queries using offline parsing

### AI Settings

- **EnableAIParsing** (true/false, default: false)  
  Use AI to parse complex mathematical expressions

- **EnableAiCalculations** (true/false, default: false)  
  Use AI for advanced calculations and explanations

- **OfflineFirst** (true/false, default: true)  
  Try offline processing before using AI

- **AiTimeout** (1000-60000, default: 10000)  
  Maximum time to wait for AI response in milliseconds

### AI Provider Settings

**Note:** Only one AI provider can be active at a time.

- **AiProvider** ("OpenAI"/"Anthropic", default: "OpenAI")  
  Primary AI provider to use

- **UseHuggingFace** (true/false, default: false)  
  Enable HuggingFace as AI provider

- **UseOpenRouter** (true/false, default: false)  
  Enable OpenRouter as AI provider

### OpenAI Settings

- **OpenAiApiKey** (string, default: null)  
  Your OpenAI API key

- **OpenAiModel** (string, default: "gpt-4")  
  OpenAI model to use (e.g., "gpt-4", "gpt-3.5-turbo")

### Anthropic Settings

- **AnthropicApiKey** (string, default: null)  
  Your Anthropic API key

- **AnthropicModel** (string, default: "claude-3-sonnet-20240229")  
  Anthropic model to use

### HuggingFace Settings

- **HuggingFaceApiKey** (string, default: null)  
  Your HuggingFace API key

- **HuggingFaceModel** (string, default: "microsoft/DialoGPT-medium")  
  HuggingFace model to use

### OpenRouter Settings

- **OpenRouterApiKey** (string, default: null)  
  Your OpenRouter API key

- **OpenRouterModel** (string, default: "anthropic/claude-3-haiku")  
  OpenRouter model to use

## Example Configuration

```json
{
  "maxResults": 5,
  "precision": 10,
  "angleUnit": 1,
  "theme": "Auto",
  "enableHistory": true,
  "historyLimit": 100,
  "enableNaturalLanguage": false,
  "enableAIParsing": false,
  "enableAiCalculations": false,
  "aiProvider": "OpenAI",
  "useHuggingFace": false,
  "useOpenRouter": false,
  "openAiApiKey": "sk-...",
  "openAiModel": "gpt-4",
  "anthropicApiKey": null,
  "anthropicModel": "claude-3-sonnet-20240229",
  "huggingFaceApiKey": null,
  "huggingFaceModel": "microsoft/DialoGPT-medium",
  "openRouterApiKey": null,
  "openRouterModel": "anthropic/claude-3-haiku",
  "aiTimeout": 10000,
  "offlineFirst": true
}
```

## Security Notes

- API keys are stored in plain text in the settings file
- Make sure to protect your settings file from unauthorized access
- Never share your settings file with API keys included
- The plugin logs settings with API keys masked (shown as "***")
