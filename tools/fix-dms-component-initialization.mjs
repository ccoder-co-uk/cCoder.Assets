import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repositoryPath = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    "..");
const componentPath = path.join(
    repositoryPath,
    "Data",
    "ccoder.co.uk",
    "DMS",
    "Components");

await moveMetadataRegistrationIntoInit(
    path.join(componentPath, "DMS.json"),
    "DMS = {",
    "    init: async function (app, container) {");
await moveMetadataRegistrationIntoInit(
    path.join(componentPath, "FolderManagement.json"),
    "var FolderManagement = {",
    "    init: async function (app, container, folder, readOnly, disablePaging=false) {");

async function moveMetadataRegistrationIntoInit(
    file,
    componentDeclaration,
    initDeclaration) {
    const component = JSON.parse(await fs.readFile(file, "utf8"));
    const registrationEnd = component.Script.indexOf(componentDeclaration);

    if (!component.Script.startsWith("api.addToMetaCache([")
        || registrationEnd < 0) {
        return;
    }

    const registration = component.Script
        .slice(0, registrationEnd)
        .trim();
    const componentScript = component.Script.slice(registrationEnd);
    const initPosition =
        componentScript.indexOf(initDeclaration) + initDeclaration.length;

    if (initPosition < initDeclaration.length) {
        throw new Error(`Unable to find init declaration in ${file}.`);
    }

    component.Script =
        componentScript.slice(0, initPosition) +
        `\r\n        ${registration.replaceAll("\r\n", "\r\n        ")}` +
        componentScript.slice(initPosition);

    await fs.writeFile(
        file,
        `${JSON.stringify(component, null, 2)}\n`,
        "utf8");
}
