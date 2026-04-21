# Target Architecture / Product Requirements

## Domain Overview

This project (`CSharpVisionAI`) demonstrates the secure integration of a multimodal AI vision model (like Gemini 1.5 Pro) into a modern C# Web Application. It is designed to be showcased to technical recruiters, emphasizing clean architectural patterns, dependency injection, and secure credential handling. 

The immediate goal is to transform the standalone Console loop into a lightweight ASP.NET Core Minimal API backed by a polished, vibrant Vanilla JavaScript and CSS frontend.

## Data Sources / Requirements

- **Backend API:** An endpoint `POST /api/vision/analyze` must accept `multipart/form-data` containing an uploaded image and a text prompt.
- **Frontend Interaction:**
  - Drag-and-drop or select file input for the image.
  - Text input field for the prompt.
  - Submit button to push formData to the server.
- **Security Constraint:** API Keys to external AI models are NOT to be hardcoded, but pulled from environment variables (e.g., `AI_VISION_API_KEY`). At this phase, the system should operate flawlessly even if the AI backend is using the local mocked delay simulation.

## Success Criteria

**The run is considered "Done" when:**
- The application boots via `dotnet run` into an ASP.NET Core dev webserver serving static files.
- The UI page is accessible at `http://localhost:<port>/index.html`.
- The user can upload an image and prompt, which are successfully passed to the Minimal API.
- The Minimal API delegates to `IVisionAgent` seamlessly.
- The frontend receives the JSON response and displays the "simulated/mocked AI output" or the real AI output on the dashboard gracefully without reloading the page.
- The UI design feels extremely premium, using a vaguely corporate sober aesthetic (professional grays/blues, clean typography, refined hover states, and subtle glassmorphism).

## Manual Verification Steps

1. Run `dotnet run` in the `CSharpVisionAI` folder.
2. Open the resulting `http://localhost:<port>` url.
3. Observe the professional, corporate-sober aesthetic of the design.
4. Upload any sample image and provide a prompt.
5. Click Submit and verify the C# backend logs the incoming request, triggers `VisionModelClient`, and pushes the response back to the front-end dashboard visually.

## Technical Constraints

- **Backend Framework:** C# ASP.NET Core Minimal API (`Microsoft.NET.Sdk.Web`). No heavy MVC or Razor Pages.
- **Frontend Framework:** STRICTLY Vanilla HTML, CSS, and JS (`wwwroot/index.html`, `styles.css`, `app.js`). No React, Angular, Tailwind, or Vue.
- **Agent Flow Compatibility:** Ensure the `Program.cs` and interface dependencies (`IVisionAgent`) remain decoupled so that an orchestrating harness can execute tests seamlessly.
- **Design Language:** Vaguely corporate sober. Avoid neon or excessively high-vibrancy palettes. Prioritize readability and professional presentation.

## Preferred Stack

- Language: C# .NET 8.0, HTML5, Vanilla CSS3, Vanilla ES6 JavaScript.
- Architecture: Minimal API backend + Static files. 

## Skills

- `frontend-design` (For generating the vibrant, premium responsive UI layout).

## Out of Scope

- Integrating database persistence.
- User authentication/login screens.
- Complex SPA routing (keep it a single index page).
