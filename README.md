# Secure AI Vision Integration (C#)

This repository demonstrates the secure integration of a multimodal AI vision model (e.g., Gemini 1.5 Pro, GPT-4o) into a C# application. It is specifically designed with architectural best practices that allow it to be easily securely plugged into an autonomous coding agent harness (like `loop-design-build`) or standard CI/CD pipeline.

## 🎯 Key Objectives

1. **Secure by Default:** Avoids hardcoding any API strings or secrets. Credentials are systematically extracted from environment variables (mapping cleanly to CI/CD Vaults/Secrets).
2. **Dependency Injection Ready:** The `IVisionAgent` interface and `HttpClient` usage are structured to directly plug into standard .NET Core DI containers.
3. **Agentic Harness Friendly:** The code architecture is decoupled, allowing an orchestration agent to abstract the AI interactions and run unit tests deterministically.

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)

### Run the Web Demo

```bash
# Optional: Set your real AI Vision API Key
# If omitted, the demo will use a mock response to ensure it executes seamlessly.
export AI_VISION_API_KEY="your-secure-key"

# Navigate into the project
cd CSharpVisionAI

# Start the web server
dotnet run
```

Once started, open your browser and navigate to **`http://localhost:5000`** (or the URL displayed in your terminal). You can then drag-and-drop an image and submit a prompt to see the secure vision analysis in action.

### Running Tests

The solution includes a dedicated test harness to verify the API boundary and project logic:

```bash
cd CSharpVisionAI.Tests
dotnet run
```

## 🏗️ Architecture

```csharp
public interface IVisionAgent
{
    Task<string> AnalyzeImageAsync(string imagePath, string prompt);
}
```

The application leverages standard `System.Net.Http` along with `System.Text.Json` to communicate with standard AI Model REST APIs, ensuring zero heavy vendor-locked SDKs unless absolutely necessary for your business logic domain.
