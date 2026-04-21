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

function tryParseJson(value) {
    if (typeof value !== "string") {
        return value;
    }

    const trimmed = value.trim();
    const fencedJson = trimmed.match(/^```(?:json)?\s*([\s\S]*?)\s*```$/i);
    const candidate = fencedJson ? fencedJson[1].trim() : trimmed;

    if (!candidate.startsWith("{") && !candidate.startsWith("[")) {
        return value;
    }

    try {
        return JSON.parse(candidate);
    } catch {
        return value;
    }
}

function extractProviderText(value) {
    const parsed = tryParseJson(value);

    if (typeof parsed === "string") {
        return parsed;
    }

    if (Array.isArray(parsed)) {
        return parsed.map(extractProviderText).filter(Boolean).join("\n");
    }

    if (!parsed || typeof parsed !== "object") {
        return String(parsed ?? "");
    }

    if (Array.isArray(parsed.choices)) {
        return parsed.choices
            .map((choice) => extractProviderText(choice.message?.content ?? choice.text ?? choice.delta?.content))
            .filter(Boolean)
            .join("\n");
    }

    if (Array.isArray(parsed.candidates)) {
        return parsed.candidates
            .map((candidate) => extractProviderText(candidate.content?.parts ?? candidate.output ?? candidate.text))
            .filter(Boolean)
            .join("\n");
    }

    if (Array.isArray(parsed.parts)) {
        return parsed.parts.map((part) => extractProviderText(part.text ?? part)).filter(Boolean).join("\n");
    }

    if (Array.isArray(parsed.content)) {
        return parsed.content.map((part) => extractProviderText(part.text ?? part)).filter(Boolean).join("\n");
    }

    const directText = parsed.analysis ?? parsed.result ?? parsed.response ?? parsed.output ?? parsed.message ?? parsed.text ?? parsed.description;
    if (directText !== undefined) {
        return extractProviderText(directText);
    }

    return JSON.stringify(parsed, null, 2);
}

function titleFromKey(key) {
    return key
        .replace(/[_-]+/g, " ")
        .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function parseMarkdownSections(text) {
    const sections = [];
    let current = { title: "Summary", lines: [], items: [] };

    text.split(/\r?\n/).forEach((line) => {
        const trimmed = line.trim();
        const heading = trimmed.match(/^(#{1,4})\s+(.+)$/);
        const bullet = trimmed.match(/^[-*]\s+(.+)$/);
        const numbered = trimmed.match(/^\d+[.)]\s+(.+)$/);

        if (heading) {
            if (current.lines.length > 0 || current.items.length > 0) {
                sections.push(current);
            }

            current = { title: heading[2], lines: [], items: [] };
            return;
        }

        if (bullet || numbered) {
            current.items.push((bullet || numbered)[1]);
            return;
        }

        if (trimmed) {
            current.lines.push(trimmed.replace(/^\*\*(.+)\*\*$/, "$1"));
        }
    });

    if (current.lines.length > 0 || current.items.length > 0) {
        sections.push(current);
    }

    return sections.length > 0 ? sections : [{ title: "Analysis", lines: [text], items: [] }];
}

function formatObjectSections(value) {
    return Object.entries(value)
        .filter(([, sectionValue]) => sectionValue !== null && sectionValue !== undefined && sectionValue !== "")
        .map(([key, sectionValue]) => {
            if (Array.isArray(sectionValue)) {
                return {
                    title: titleFromKey(key),
                    lines: [],
                    items: sectionValue.map((item) => extractProviderText(item))
                };
            }

            if (typeof sectionValue === "object") {
                return {
                    title: titleFromKey(key),
                    lines: [JSON.stringify(sectionValue, null, 2)],
                    items: []
                };
            }

            return {
                title: titleFromKey(key),
                lines: [String(sectionValue)],
                items: []
            };
        });
}

function formatVisionResponse(payload) {
    const rawAnalysis = payload?.analysis ?? payload;
    const parsed = tryParseJson(rawAnalysis);

    if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) {
        const providerText = extractProviderText(parsed);
        const isFallbackJson = providerText.trim().startsWith("{");
        return {
            sections: isFallbackJson ? formatObjectSections(parsed) : parseMarkdownSections(providerText),
            raw: providerText
        };
    }

    const text = extractProviderText(parsed);
    const nested = tryParseJson(text);

    if (nested && typeof nested === "object" && !Array.isArray(nested)) {
        return {
            sections: formatObjectSections(nested),
            raw: JSON.stringify(nested, null, 2)
        };
    }

    return {
        sections: parseMarkdownSections(text),
        raw: text
    };
}

function appendTextBlock(parent, text) {
    const paragraph = document.createElement("p");
    paragraph.textContent = text;
    parent.appendChild(paragraph);
}

function renderVisionResponse(payload) {
    const formatted = formatVisionResponse(payload);
    resultCopy.replaceChildren();

    formatted.sections.forEach((section) => {
        const sectionElement = document.createElement("section");
        sectionElement.className = "markdown-section";

        const heading = document.createElement("h3");
        heading.textContent = section.title;
        sectionElement.appendChild(heading);

        section.lines.forEach((line) => appendTextBlock(sectionElement, line));

        if (section.items.length > 0) {
            const list = document.createElement("ul");
            list.className = "result-list";
            section.items.forEach((item) => {
                const listItem = document.createElement("li");
                listItem.textContent = item;
                list.appendChild(listItem);
            });
            sectionElement.appendChild(list);
        }

        resultCopy.appendChild(sectionElement);
    });
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
    fileDetail.textContent = `${file.type || "Image file"} - ${formatBytes(file.size)}`;
    fileMetric.textContent = `${file.name} - ${formatBytes(file.size)}`;
}

function updatePromptMetric() {
    const promptLength = promptInput.value.trim().length;
    promptMetric.textContent = promptLength > 0 ? `${promptLength} prompt characters` : "Prompt pending";
}

function showError(message) {
    setStatus("Needs attention", "error");
    formMessage.textContent = message;
    resultCopy.replaceChildren();
    appendTextBlock(resultCopy, message);
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
    resultCopy.replaceChildren();
    appendTextBlock(resultCopy, "Analysis in progress...");
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

        renderVisionResponse(payload);
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
