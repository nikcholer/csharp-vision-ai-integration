using System.Text;
using CSharpVisionAI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

var tests = new EndpointTests();
await tests.ValidMultipartRequestDelegatesToVisionAgent();
await tests.MissingImageReturnsBadRequest();
await tests.BlankPromptReturnsBadRequest();
await tests.LiveServerAcceptsMultipartRequest();

Console.WriteLine("All endpoint tests passed.");

internal sealed class EndpointTests
{
    public async Task ValidMultipartRequestDelegatesToVisionAgent()
    {
        var agent = new RecordingVisionAgent("mocked analysis");
        var request = CreateMultipartRequest(
            fileName: "sample.jpg",
            contentType: "image/jpeg",
            imageBytes: [0x01, 0x02, 0x03],
            prompt: "Describe the image");

        var result = await VisionApi.HandleAnalyzeAsync(request, agent, NullLoggerFactory.Instance.CreateLogger("test"));

        AssertEqual(StatusCodes.Status200OK, result.StatusCode, "Expected a successful status code.");
        AssertEqual("Describe the image", agent.Prompt, "Expected the prompt to be delegated unchanged.");
        AssertTrue(agent.ImageFileExistedWhenCalled, "Expected a temporary image path to be delegated.");
        AssertEqual(3L, agent.ImageLengthWhenCalled, "Expected the uploaded image bytes to be written before delegation.");
        AssertContains("mocked analysis", result.Json, "Expected the analysis output in the JSON response.");
    }

    public async Task MissingImageReturnsBadRequest()
    {
        var agent = new RecordingVisionAgent("unused");
        var request = CreateMultipartRequest(fileName: null, contentType: null, imageBytes: null, prompt: "Describe it");

        var result = await VisionApi.HandleAnalyzeAsync(request, agent, NullLoggerFactory.Instance.CreateLogger("test"));

        AssertEqual(StatusCodes.Status400BadRequest, result.StatusCode, "Expected a bad request for a missing image.");
        AssertEqual(0, agent.CallCount, "Expected the vision agent not to be called.");
        AssertContains("image", result.Json, "Expected the validation response to mention the image field.");
    }

    public async Task BlankPromptReturnsBadRequest()
    {
        var agent = new RecordingVisionAgent("unused");
        var request = CreateMultipartRequest(
            fileName: "sample.jpg",
            contentType: "image/jpeg",
            imageBytes: [0x01],
            prompt: " ");

        var result = await VisionApi.HandleAnalyzeAsync(request, agent, NullLoggerFactory.Instance.CreateLogger("test"));

        AssertEqual(StatusCodes.Status400BadRequest, result.StatusCode, "Expected a bad request for a blank prompt.");
        AssertEqual(0, agent.CallCount, "Expected the vision agent not to be called.");
        AssertContains("prompt", result.Json, "Expected the validation response to mention the prompt field.");
    }

    public async Task LiveServerAcceptsMultipartRequest()
    {
        var port = Random.Shared.Next(5100, 5199);
        await using var app = VisionApp.Create(["--urls", $"http://127.0.0.1:{port}"]);
        await app.StartAsync();

        try
        {
            using var client = new HttpClient();
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent([0x01, 0x02, 0x03]), "image", "sample.jpg");
            form.Add(new StringContent("Describe the smoke test image", Encoding.UTF8), "prompt");

            var response = await client.PostAsync($"http://127.0.0.1:{port}/api/vision/analyze", form);
            var json = await response.Content.ReadAsStringAsync();

            AssertEqual(StatusCodes.Status200OK, (int)response.StatusCode, "Expected the live endpoint to accept multipart input.");
            AssertContains("SUCCESS", json, "Expected the live endpoint to return the mocked analysis.");
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static HttpRequest CreateMultipartRequest(string? fileName, string? contentType, byte[]? imageBytes, string prompt)
    {
        var context = new DefaultHttpContext();
        var formFields = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["prompt"] = prompt
        };

        var files = new FormFileCollection();
        if (fileName is not null && contentType is not null && imageBytes is not null)
        {
            var stream = new MemoryStream(imageBytes);
            files.Add(new FormFile(stream, 0, stream.Length, "image", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            });
        }

        context.Request.ContentType = "multipart/form-data; boundary=test";
        context.Request.Form = new FormCollection(formFields, files);
        return context.Request;
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertContains(string expected, string actual, string message)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{message} Response: {actual}");
        }
    }
}

internal sealed class RecordingVisionAgent(string response) : IVisionAgent
{
    public int CallCount { get; private set; }
    public string? ImagePath { get; private set; }
    public string? Prompt { get; private set; }
    public bool ImageFileExistedWhenCalled { get; private set; }
    public long ImageLengthWhenCalled { get; private set; }

    public Task<string> AnalyzeImageAsync(string imagePath, string prompt)
    {
        CallCount++;
        ImagePath = imagePath;
        Prompt = prompt;
        ImageFileExistedWhenCalled = File.Exists(imagePath);
        ImageLengthWhenCalled = ImageFileExistedWhenCalled ? new FileInfo(imagePath).Length : 0;
        return Task.FromResult(response);
    }
}
