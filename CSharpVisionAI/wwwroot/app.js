const form = document.getElementById("analysis-form");
const dropZone = document.getElementById("drop-zone");
const imageInput = document.getElementById("image-input");
const promptInput = document.getElementById("prompt-input");
const submitButton = document.getElementById("submit-button");
const formMessage = document.getElementById("form-message");
const statusPill = document.getElementById("status-pill");
const resultPanel = document.querySelector(".result-panel");
const resultCopy = document.getElementById("result-copy");
const fileName = document.getElementById("file-name");
const fileDetail = document.getElementById("file-detail");
const fileMetric = document.getElementById("file-metric");
const promptMetric = document.getElementById("prompt-metric");

const endpoint = "/api/vision/analyze";

function selectedFile() {
    return imageInput.files && imageInput.files.length > 0 ? imageInput.files[0] : null;
}

function formatBytes(bytes) {
    if (bytes < 1024) {
        return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
        return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function setStatus(label, state) {
    statusPill.textContent = label;
    statusPill.classList.remove("is-loading", "is-success", "is-error");
    resultPanel.classList.remove("is-loading");
    formMessage.classList.remove("is-error");

    if (state) {
        statusPill.classList.add(`is-${state}`);
    }

    if (state === "loading") {
        resultPanel.classList.add("is-loading");
    }

    if (state === "error") {
        formMessage.classList.add("is-error");
    }
}

function updateFileSummary() {
    const file = selectedFile();

    if (!file) {
        dropZone.classList.remove("has-file");
        fileName.textContent = "Select or drag a visual asset";
        fileDetail.textContent = "JPG, PNG, WEBP, or GIF files can be prepared for analysis.";
        fileMetric.textContent = "File pending";
        return;
    }

    dropZone.classList.add("has-file");
    fileName.textContent = file.name;
    fileDetail.textContent = `${file.type || "Image file"} · ${formatBytes(file.size)}`;
    fileMetric.textContent = `${file.name} · ${formatBytes(file.size)}`;
}

function updatePromptMetric() {
    const promptLength = promptInput.value.trim().length;
    promptMetric.textContent = promptLength > 0 ? `${promptLength} prompt characters` : "Prompt pending";
}

function showError(message) {
    setStatus("Needs attention", "error");
    formMessage.textContent = message;
    resultCopy.textContent = message;
}

async function submitAnalysis(event) {
    event.preventDefault();

    const file = selectedFile();
    const prompt = promptInput.value.trim();

    if (!file) {
        showError("Select an image before requesting analysis.");
        return;
    }

    if (!prompt) {
        showError("Enter a prompt before requesting analysis.");
        return;
    }

    const formData = new FormData();
    formData.append("image", file);
    formData.append("prompt", prompt);

    submitButton.disabled = true;
    submitButton.textContent = "Analyzing...";
    formMessage.textContent = "Uploading image and awaiting the vision response.";
    resultCopy.textContent = "Analysis in progress...";
    setStatus("Analyzing", "loading");

    try {
        const response = await fetch(endpoint, {
            method: "POST",
            body: formData
        });

        const payload = await response.json().catch(() => ({}));

        if (!response.ok) {
            throw new Error(payload.error || `Request failed with status ${response.status}.`);
        }

        resultCopy.textContent = payload.analysis || JSON.stringify(payload, null, 2);
        fileMetric.textContent = payload.fileName || fileMetric.textContent;
        promptMetric.textContent = payload.prompt ? `${payload.prompt.length} prompt characters` : promptMetric.textContent;
        formMessage.textContent = "Analysis complete.";
        setStatus("Complete", "success");
    } catch (error) {
        showError(error instanceof Error ? error.message : "The analysis request could not be completed.");
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = "Analyze image";
    }
}

dropZone.addEventListener("dragover", (event) => {
    event.preventDefault();
    dropZone.classList.add("is-dragging");
});

dropZone.addEventListener("dragleave", () => {
    dropZone.classList.remove("is-dragging");
});

dropZone.addEventListener("drop", (event) => {
    event.preventDefault();
    dropZone.classList.remove("is-dragging");

    if (event.dataTransfer.files.length > 0) {
        imageInput.files = event.dataTransfer.files;
        updateFileSummary();
    }
});

imageInput.addEventListener("change", updateFileSummary);
promptInput.addEventListener("input", updatePromptMetric);
form.addEventListener("submit", submitAnalysis);
