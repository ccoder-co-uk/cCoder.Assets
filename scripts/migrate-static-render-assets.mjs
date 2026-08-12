import { readdir, readFile, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repositoryDirectory = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "..");
const dataDirectory = path.join(repositoryDirectory, "Data");

const staticScriptPrefixes = [
    "Bundle.Monaco",
    "Core.",
    "Dependency.",
    "Monaco.",
    "Widgets.",
    "Workflow."
];

const codeEditorPages = new Set([
    "Admin/AppManagement",
    "Admin/PlatformAdmin/CommonCacheEndpoint",
    "Admin/PlatformAdmin/Tenants",
    "Admin/WorkflowDesigner",
    "Admin/Workflows",
    "Admin/Workflows/Editor"
]);

const workflowPages = new Set([
    "Admin/WorkflowDesigner",
    "Admin/Workflows",
    "Admin/Workflows/Editor"
]);

async function filesBelow(directory) {
    const entries = await readdir(directory, { withFileTypes: true });
    const files = [];

    for (const entry of entries) {
        const entryPath = path.join(directory, entry.name);

        if (entry.isDirectory()) {
            files.push(...await filesBelow(entryPath));
        } else {
            files.push(entryPath);
        }
    }

    return files;
}

function isStaticScript(name) {
    return name === "Background"
        || staticScriptPrefixes.some(prefix => name.startsWith(prefix));
}

function removeStaticPlaceholders(value) {
    if (typeof value === "string") {
        return value
            .replace(/\[script\[([^\]]+)\]\]\r?\n?/g, (match, name) =>
                isStaticScript(name) ? "" : match)
            .replace(/\[style\[((?:Dependency\.|Bundle\.Monaco)[^\]]*)\]\]\r?\n?/g, "")
            .replace(/<script nonce="\[request\[nonce\]\]">\s*<\/script>\r?\n?/g, "");
    }

    if (Array.isArray(value)) {
        return value.map(removeStaticPlaceholders);
    }

    if (value && typeof value === "object") {
        for (const [key, child] of Object.entries(value)) {
            value[key] = removeStaticPlaceholders(child);
        }
    }

    return value;
}

function addLayoutAssets(layout) {
    if (!layout.HeaderHtml.includes("/everything.min.css")) {
        const stylesheet = '<link rel="stylesheet" href="/everything.min.css" />\n';
        const stylePosition = layout.HeaderHtml.indexOf("<style");

        layout.HeaderHtml = stylePosition >= 0
            ? layout.HeaderHtml.slice(0, stylePosition)
                + stylesheet
                + layout.HeaderHtml.slice(stylePosition)
            : stylesheet + layout.HeaderHtml;
    }

    if (!layout.Html.includes("/framework.min.js")) {
        layout.Html = '<script nonce="[request[nonce]]" src="/framework.min.js"></script>\n'
            + layout.Html;
    }
}

function addPageAssets(page) {
    const assetTags = [];

    if (codeEditorPages.has(page.Path)) {
        assetTags.push('<link rel="stylesheet" href="/code-editor.min.css" />');
        assetTags.push('<script nonce="[request[nonce]]" src="/code-editor.min.js"></script>');
    }

    if (workflowPages.has(page.Path)) {
        assetTags.push('<script nonce="[request[nonce]]" src="/workflow.min.js"></script>');
    }

    if (assetTags.length === 0 || !Array.isArray(page.Contents)) {
        return;
    }

    const body = page.Contents.find(content =>
        content.CultureId === "" && content.Name.toLowerCase() === "body");

    if (body && !body.Html.includes(assetTags[0])) {
        body.Html = assetTags.join("\n") + "\n" + body.Html;
    }
}

const jsonFiles = (await filesBelow(dataDirectory))
    .filter(file => file.endsWith(".json") && !file.includes(`${path.sep}Packages${path.sep}`));

for (const file of jsonFiles) {
    const original = await readFile(file, "utf8");
    const originalValue = JSON.parse(original);
    const value = removeStaticPlaceholders(structuredClone(originalValue));
    const normalized = file.replaceAll("\\", "/");

    if (normalized.includes("/Layouts/")) {
        addLayoutAssets(value);
    }

    if (normalized.includes("/Pages/")) {
        addPageAssets(value);
    }

    if (JSON.stringify(value) !== JSON.stringify(originalValue)) {
        await writeFile(file, JSON.stringify(value, null, 2) + "\n");
    }
}

const cacheFiles = (await filesBelow(dataDirectory))
    .filter(file => file.endsWith(".json") && file.includes(`${path.sep}Common Cache${path.sep}`));

for (const file of cacheFiles) {
    const normalized = file.replaceAll("\\", "/");
    const baseName = path.basename(file, ".json");
    const isScript = normalized.includes("/Scripts/");
    const isStyle = normalized.includes("/Styles/");

    if ((isScript && isStaticScript(baseName))
        || (isStyle && (baseName.startsWith("Dependency.") || baseName.startsWith("Bundle.Monaco")))) {
        await rm(file);
    }
}
