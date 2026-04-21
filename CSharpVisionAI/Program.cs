using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSharpVisionAI
{
    public interface IVisionAgent
    {
        Task<string> AnalyzeImageAsync(string imagePath, string prompt);
    }

    /// <summary>
    /// Demonstrates securely invoking an AI Image Model (e.g., Gemini Vision, GPT-4o)
    /// using standard .NET patterns, dependency injection, and environment-driven secrets.
    /// </summary>
    public class VisionModelClient : IVisionAgent
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly bool _useMockResponse;

        public VisionModelClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Securely load API Key from Environment Variable / Key Vault rather than hardcoding.
            _apiKey = Environment.GetEnvironmentVariable("AI_VISION_API_KEY")
                      ?? throw new InvalidOperationException("Missing environment variable: AI_VISION_API_KEY");

            var configuredEndpoint = Environment.GetEnvironmentVariable("AI_VISION_ENDPOINT");
            _endpoint = string.IsNullOrWhiteSpace(configuredEndpoint)
                ? "https://api.example-ai-provider.com/v1/vision/analyze"
                : configuredEndpoint;
            _model = Environment.GetEnvironmentVariable("AI_VISION_MODEL") ?? "vision-model-v1";
            _useMockResponse = string.IsNullOrWhiteSpace(configuredEndpoint)
                || string.Equals(_apiKey, "dummy_key_for_demo_build", StringComparison.Ordinal);
        }

        public async Task<string> AnalyzeImageAsync(string imagePath, string prompt)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image not found", imagePath);
            }

            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                        }
                    }
                }
            };

            if (_useMockResponse)
            {
                await Task.Delay(500);
                return "SUCCESS: Image analyzed via secure channel. (Mocked response for demo purposes)";
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    public sealed record VisionApiResult(int StatusCode, object Payload)
    {
        public string Json => JsonSerializer.Serialize(Payload);

        public IResult ToHttpResult()
        {
            return Results.Json(Payload, statusCode: StatusCode);
        }
    }

    public static class VisionApi
    {
        public static async Task<VisionApiResult> HandleAnalyzeAsync(
            HttpRequest request,
            IVisionAgent visionAgent,
            ILogger logger)
        {
            if (!request.HasFormContentType)
            {
                return BadRequest("form", "Expected a multipart form request.");
            }

            var form = await request.ReadFormAsync();
            var image = form.Files.GetFile("image");
            var prompt = form["prompt"].ToString();

            if (image is null || image.Length == 0)
            {
                return BadRequest("image", "An uploaded image is required.");
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return BadRequest("prompt", "A prompt is required.");
            }

            var extension = Path.GetExtension(image.FileName);
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");

            try
            {
                await using (var output = File.Create(tempPath))
                {
                    await image.CopyToAsync(output);
                }

                logger.LogInformation(
                    "Analyzing uploaded image {FileName} ({Length} bytes).",
                    image.FileName,
                    image.Length);

                var analysis = await visionAgent.AnalyzeImageAsync(tempPath, prompt.Trim());

                return new VisionApiResult(
                    StatusCodes.Status200OK,
                    new
                    {
                        analysis,
                        fileName = image.FileName,
                        prompt = prompt.Trim()
                    });
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static VisionApiResult BadRequest(string field, string message)
        {
            return new VisionApiResult(
                StatusCodes.Status400BadRequest,
                new
                {
                    error = message,
                    field
                });
        }
    }

    public static class VisionApp
    {
        public static WebApplication Create(string[] args)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = ResolveContentRoot()
            });

            // Keep the demo runnable without local secret setup while preserving environment-based credentials.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI_VISION_API_KEY")))
            {
                Console.WriteLine("[WARN] AI_VISION_API_KEY not found. Injecting dummy key for demonstration server...");
                Environment.SetEnvironmentVariable("AI_VISION_API_KEY", "dummy_key_for_demo_build");
            }

            builder.Services.AddHttpClient<IVisionAgent, VisionModelClient>();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
            app.MapPost(
                "/api/vision/analyze",
                async (HttpRequest request, IVisionAgent visionAgent, ILoggerFactory loggerFactory) =>
                {
                    var response = await VisionApi.HandleAnalyzeAsync(
                        request,
                        visionAgent,
                        loggerFactory.CreateLogger("VisionApi"));
                    return response.ToHttpResult();
                });

            return app;
        }

        private static string ResolveContentRoot()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "CSharpVisionAI.csproj")))
                {
                    return current.FullName;
                }

                var nestedProject = Path.Combine(current.FullName, "CSharpVisionAI", "CSharpVisionAI.csproj");
                if (File.Exists(nestedProject))
                {
                    return Path.Combine(current.FullName, "CSharpVisionAI");
                }

                current = current.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var app = VisionApp.Create(args);

            Console.WriteLine("--- Secure AI Vision Web Application initialized ---");
            await app.RunAsync();
        }
    }
}
