# Project Backlog

## High Priority Queue
- [x] **Infrastructure Setup:** Modify `CSharpVisionAI.csproj` to use `Microsoft.NET.Sdk.Web`. Rewrite `Program.cs` to leverage `WebApplication.CreateBuilder`, setup Dependency Injection for `IVisionAgent`, and enable `app.UseStaticFiles()` and `app.UseDefaultFiles()`.
- [x] **Backend Web API:** Create a structured Minimal API endpoint `POST /api/vision/analyze`. Configure it to accept a multipart form payload (image buffer and text prompt), invoke `_visionAgent.AnalyzeImageAsync`, and return the structured JSON payload. Ensure logging is captured.
- [x] **Frontend Foundation & Aesthetics:** Scaffold `wwwroot/index.html` and `wwwroot/styles.css`. Implement a premium, vaguely corporate sober UI featuring professional typography, clean grid layout, drag-and-drop capability, and refined glassmorphism panels. Avoid neon/vibrant palettes.
- [x] **Frontend Integration:** Scaffold `wwwroot/app.js`. Wire up event listeners to capture the image and prompt, construct the `FormData`, and make the asynchronous `fetch` call to `/api/vision/analyze`. Integrate professional loading indicators and gracefully handle error states.

## Medium Priority Queue
- [ ] **Real API Swap:** Transition the backend `IVisionAgent` implementation from the mocked local delay to actually calling out to a Live Vision AI endpoint using the injected API keys.
- [ ] **Response Parsing:** Gracefully parse complex markdown/object responses from the ML service in the frontend.

## Icebox (Human-Controlled: Agent Read-Only)
- [ ] Explore adding conversation memory so follow up questions can be asked about an uploaded image.
- [ ] Add Dockerfile support for automated container deployments.
