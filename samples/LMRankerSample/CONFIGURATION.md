# LMRanker Sample Configuration Examples

This file provides quick-start configuration examples for different AI services.

## Environment Variables

### Azure OpenAI

```bash
# Windows PowerShell
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_API_KEY="your-api-key"
$env:AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4"

# Windows Command Prompt
set AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
set AZURE_OPENAI_API_KEY=your-api-key
set AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4

# Linux/macOS
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
export AZURE_OPENAI_API_KEY="your-api-key"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4"
```

### OpenAI

```bash
# Windows PowerShell
$env:OPENAI_API_KEY="your-openai-api-key"
$env:OPENAI_MODEL="gpt-4"

# Windows Command Prompt
set OPENAI_API_KEY=your-openai-api-key
set OPENAI_MODEL=gpt-4

# Linux/macOS
export OPENAI_API_KEY="your-openai-api-key"
export OPENAI_MODEL="gpt-4"
```

## Code Configuration

### Azure OpenAI Configuration

```csharp
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4";

if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
{
    builder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: endpoint,
        apiKey: apiKey
    );
    Console.WriteLine($"✅ Configured Azure OpenAI: {deploymentName}");
    return true;
}
```

### OpenAI Configuration

```csharp
var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4";

if (!string.IsNullOrEmpty(openAIKey))
{
    builder.AddOpenAIChatCompletion(
        modelId: model,
        apiKey: openAIKey
    );
    Console.WriteLine($"✅ Configured OpenAI: {model}");
    return true;
}
```

### Local Model (Ollama) Configuration

```csharp
try
{
    builder.AddOpenAIChatCompletion(
        modelId: "llama3.1",
        endpoint: new Uri("http://localhost:11434"),
        apiKey: "not-needed-for-local"
    );
    Console.WriteLine("✅ Configured local model via Ollama");
    return true;
}
catch
{
    Console.WriteLine("⚠️  Could not connect to local model");
}
```

## Quick Setup

1. **Choose your AI service** (Azure OpenAI, OpenAI, or local model)
2. **Set environment variables** using the examples above
3. **Uncomment the relevant configuration** in `Program.cs` → `ConfigureAIService()` method
4. **Run the sample**: `dotnet run`

## Testing Without AI Service

The sample will run and show a helpful message if no AI service is configured, making it safe to build and test the project structure without requiring API keys.
