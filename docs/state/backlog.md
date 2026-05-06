# Project Backlog

## High Priority Queue
- [x] **Infrastructure Setup:** Modify `CSharpVisionAI.csproj` to use `Microsoft.NET.Sdk.Web`. Rewrite `Program.cs` to leverage `WebApplication.CreateBuilder`, setup Dependency Injection for `IVisionAgent`, and enable `app.UseStaticFiles()` and `app.UseDefaultFiles()`.
- [x] **Backend Web API:** Create a structured Minimal API endpoint `POST /api/vision/analyze`. Configure it to accept a multipart form payload (image buffer and text prompt), invoke `_visionAgent.AnalyzeImageAsync`, and return the structured JSON payload. Ensure logging is captured.
- [x] **Frontend Foundation & Aesthetics:** Scaffold `wwwroot/index.html` and `wwwroot/styles.css`. Implement a premium, vaguely corporate sober UI featuring professional typography, clean grid layout, drag-and-drop capability, and refined glassmorphism panels. Avoid neon/vibrant palettes.
- [x] **Frontend Integration:** Scaffold `wwwroot/app.js`. Wire up event listeners to capture the image and prompt, construct the `FormData`, and make the asynchronous `fetch` call to `/api/vision/analyze`. Integrate professional loading indicators and gracefully handle error states.

## Medium Priority Queue
- [x] **Real API Swap:** Transition the backend `IVisionAgent` implementation from the mocked local delay to actually calling out to a Live Vision AI endpoint using the injected API keys.
- [x] **Response Parsing:** Gracefully parse complex markdown/object responses from the ML service in the frontend.

## Icebox (Human-Controlled: Agent Read-Only)
- [ ] Explore adding conversation memory so follow up questions can be asked about an uploaded image.
- [ ] Add Dockerfile support for automated container deployments.
- [ ] Evolve long-running AI calls into an asynchronous job queue: return `202 Accepted` with a `jobId`, process requests through a controlled worker, and track states such as `Queued`, `Running`, `Succeeded`, `Failed`, and `Cancelled`.
- [ ] Add a queue/history panel to the UI so users can submit several analyses, monitor pending jobs, retry failures, cancel queued work, and inspect completed results without opening multiple tabs.
- [ ] Consider durable production infrastructure for queued AI jobs, such as Azure Queue Storage or Service Bus for work dispatch, Blob Storage for uploaded images, SQL Server for job metadata, and SignalR or polling for live status updates.
- [ ] Add reliability and governance controls around queued AI work, including bounded retries, rate-limit handling, worker concurrency caps, idempotency keys, output validation, and status-transition logging.
