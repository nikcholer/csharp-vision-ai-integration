using System.Text;
using CSharpVisionAI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

var tests = new EndpointTests();
await tests.ValidMultipartRequestDelegatesToVisionAgent();
await tests.MissingImageReturnsBadRequest();
await tests.BlankPromptReturnsBadRequest();
await tests.LiveServerAcceptsMultipartRequest();
await tests.LiveServerServesStaticFrontend();
tests.StaticFrontendFoundationIncludesUploadWorkflow();

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

    public async Task LiveServerServesStaticFrontend()
    {
        var port = Random.Shared.Next(5200, 5299);
        await using var app = VisionApp.Create(["--urls", $"http://127.0.0.1:{port}"]);
        await app.StartAsync();

        try
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync($"http://127.0.0.1:{port}/index.html");
            var css = await client.GetStringAsync($"http://127.0.0.1:{port}/styles.css");
            var js = await client.GetStringAsync($"http://127.0.0.1:{port}/app.js");

            AssertContains("CSharpVisionAI", html, "Expected the live server to serve index.html.");
            AssertContains("--surface", css, "Expected the live server to serve styles.css.");
            AssertContains("/api/vision/analyze", js, "Expected the live server to serve app.js.");
        }
        finally
        {
            await app.StopAsync();
        }
    }

    public void StaticFrontendFoundationIncludesUploadWorkflow()
    {
        var projectRoot = FindProjectRoot();
        var indexPath = Path.Combine(projectRoot, "CSharpVisionAI", "wwwroot", "index.html");
        var stylesPath = Path.Combine(projectRoot, "CSharpVisionAI", "wwwroot", "styles.css");
        var scriptPath = Path.Combine(projectRoot, "CSharpVisionAI", "wwwroot", "app.js");

        AssertTrue(File.Exists(indexPath), "Expected wwwroot/index.html to exist.");
        AssertTrue(File.Exists(stylesPath), "Expected wwwroot/styles.css to exist.");
        AssertTrue(File.Exists(scriptPath), "Expected wwwroot/app.js to exist.");

        var html = File.ReadAllText(indexPath);
        var css = File.ReadAllText(stylesPath);
        var js = File.ReadAllText(scriptPath);

        AssertContains("type=\"file\"", html, "Expected the page to expose an image file input.");
        AssertContains("name=\"prompt\"", html, "Expected the page to expose a prompt input.");
        AssertContains("drop", html, "Expected the page copy or markup to support drag-and-drop affordance.");
        AssertContains("styles.css", html, "Expected the page to load the stylesheet.");
        AssertContains("app.js", html, "Expected the page to load the integration script.");
        AssertContains("FormData", js, "Expected the integration script to construct multipart form data.");
        AssertContains("/api/vision/analyze", js, "Expected the integration script to call the vision endpoint.");
        AssertContains("fetch", js, "Expected the integration script to submit asynchronously.");
        AssertContains("is-loading", js, "Expected the integration script to manage loading state.");
        AssertContains("error", js, "Expected the integration script to handle error states.");
        AssertContains("glass", css, "Expected the stylesheet to include refined glass panel styling.");
        AssertContains("--surface", css, "Expected the stylesheet to define a sober surface color system.");
    }

    private static string FindProjectRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "docs", "planning.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
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
