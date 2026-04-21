using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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

        public VisionModelClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Securely load API Key from Environment Variable / Key Vault rather than hardcoding.
            _apiKey = Environment.GetEnvironmentVariable("AI_VISION_API_KEY")
                      ?? throw new InvalidOperationException("Missing environment variable: AI_VISION_API_KEY");

            _endpoint = Environment.GetEnvironmentVariable("AI_VISION_ENDPOINT")
                      ?? "https://api.example-ai-provider.com/v1/vision/analyze";
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
                model = "vision-model-v1",
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

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // In a real scenario, this would execute the HTTP call:
            // var response = await _httpClient.SendAsync(request);
            // response.EnsureSuccessStatusCode();
            // return await response.Content.ReadAsStringAsync();

            // Mocking response for harness demonstration.
            await Task.Delay(500);
            return "SUCCESS: Image analyzed via secure channel. (Mocked response for demo purposes)";
        }
    }

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            Console.WriteLine("--- Secure AI Vision Web Application initialized ---");
            await app.RunAsync();
        }
    }
}
