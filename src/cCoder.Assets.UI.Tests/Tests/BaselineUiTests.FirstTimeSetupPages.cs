// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using cCoder.Assets.UI.Tests.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

public sealed partial class BaselineUiTests
{
    [Fact]
    public async Task FirstTimeSetupPages_ShouldRenderEveryComponent()
    {
        // Given
        IPage authenticatedPage = await fixture.NewPageAsync();
        BrowserDiagnosticCollector loginDiagnostics = new();
        loginDiagnostics.Attach(page: authenticatedPage);

        try
        {
            await LoginAsInitialAdministratorAsync(page: authenticatedPage);
        }
        catch
        {
            await loginDiagnostics.WriteAsync(
                page: authenticatedPage,
                artifactDirectory: Path.Combine(
                    path1: fixture.Settings.ArtifactsRoot,
                    path2: "InitialAdministratorLogin"),
                processLogs: fixture.ApplicationLogs);

            throw;
        }

        IReadOnlyList<BaselinePageContract> contracts =
            await ReadFirstTimeSetupPageContractsAsync();

        List<string> failures = [];

        // When
        foreach (BaselinePageContract contract in contracts)
        {
            IPage page = IsAnonymousSecurityPage(path: contract.Path)
                ? await fixture.NewPageAsync()
                : await authenticatedPage.Context.NewPageAsync();

            BrowserDiagnosticCollector diagnostics = new();
            diagnostics.Attach(page: page);

            try
            {
                IResponse? response = await page.GotoAsync(
                    url: new Uri(
                        baseUri: fixture.WebBaseAddress,
                        relativeUri: await ResolvePageAddressAsync(
                            path: contract.Path))
                        .ToString());

                Assert.NotNull(@object: response);

                Assert.True(
                    condition: response.Ok,
                    userMessage: $"{contract.Path} returned {response.Status}.");

                await page.Locator(selector: "main.site-main")
                    .WaitForAsync();

                string content = await page.ContentAsync();
                AssertRenderable(content: content);

                await ExerciseNonDestructiveComponentInteractionsAsync(
                    page: page);

                foreach (string componentName in contract.Components)
                {
                    if (contract.Path == "Admin/WorkflowDesigner"
                        && componentName == "FlowEditor")
                    {
                        await page.WaitForURLAsync(
                            url: url => new Uri(uriString: url).AbsolutePath
                                == "/Admin/Workflows");

                        diagnostics.Reset();

                        await page.Locator(selector: "main.site-main")
                            .WaitForAsync();

                        continue;
                    }

                    await page.Locator(
                        selector: $".component[name='{componentName}']")
                        .WaitForAsync(
                            options: new LocatorWaitForOptions
                            {
                                State = WaitForSelectorState.Attached,
                                Timeout = 10_000
                            });
                }

                diagnostics.ThrowIfBroken();
            }
            catch (Exception exception)
            {
                string artifactName = string.IsNullOrWhiteSpace(
                    value: contract.Path)
                        ? "Root"
                        : contract.Path.Replace(
                            oldChar: '/',
                            newChar: '_');

                await diagnostics.WriteAsync(
                    page: page,
                    artifactDirectory: Path.Combine(
                        path1: fixture.Settings.ArtifactsRoot,
                        path2: nameof(
                            FirstTimeSetupPages_ShouldRenderEveryComponent),
                        path3: artifactName),
                    processLogs: fixture.ApplicationLogs);

                failures.Add(
                    item: $"{contract.PathDisplay}: {exception.Message}");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        await fixture.ClosePageAsync(page: authenticatedPage);

        // Then
        Assert.True(
            condition: failures.Count == 0,
            userMessage: BuildFailureMessage(failures: failures));
    }

    private static bool IsAnonymousSecurityPage(string path) =>
        path is "Login" or "ResetPassword";

    private static Task<string> ResolvePageAddressAsync(
        string path)
    {
        if (path == "ResetPassword")
        {
            return Task.FromResult(
                result: "ResetPassword?token=preview&uid=AssetsAcceptanceAdmin");
        }

        return Task.FromResult(result: path);
    }

    private static async Task ExerciseNonDestructiveComponentInteractionsAsync(
        IPage page)
    {
        ILocator tabs = page.Locator(
            selector: "button[data-bs-toggle='tab']:visible");

        int tabCount = await tabs.CountAsync();

        for (int index = 0; index < tabCount; index++)
        {
            ILocator tab = tabs.Nth(index: index);

            if (await tab.IsEnabledAsync())
            {
                await tab.ClickAsync();
            }
        }

        ILocator expanders = page.Locator(
            selector: "button[aria-expanded='false']:visible");

        if (await expanders.CountAsync() > 0)
        {
            ILocator firstExpander = expanders.First;

            if (await firstExpander.IsEnabledAsync())
            {
                await firstExpander.ClickAsync();
            }
        }
    }

    private async Task<IReadOnlyList<BaselinePageContract>>
        ReadFirstTimeSetupPageContractsAsync()
    {
        string packagePath = Path.Combine(
            path1: fixture.Settings.AssetsRoot,
            path2: "Packages",
            path3: "First Time Setup",
            path4: "first-app.json");

        IReadOnlyDictionary<string, string> componentMarkupByName =
            await ReadBaselineComponentMarkupByNameAsync();

        using JsonDocument package = JsonDocument.Parse(
            json: await File.ReadAllTextAsync(path: packagePath));

        JsonElement pageItem = package.RootElement
            .GetProperty(propertyName: "Items")
            .EnumerateArray()
            .Single(predicate: item =>
            {
                string? itemType = item
                    .GetProperty(propertyName: "Type")
                    .GetString();

                return string.Equals(
                    a: itemType,
                    b: "ContentManagement/Page",
                    comparisonType: StringComparison.Ordinal);
            });

        string pageJson = pageItem
            .GetProperty(propertyName: "Data")
            .GetString()!;

        using JsonDocument pages = JsonDocument.Parse(
            json: pageJson);

        return pages.RootElement
            .EnumerateArray()
            .Select(selector: page => CreatePageContract(
                page: page,
                componentMarkupByName: componentMarkupByName))
            .OrderBy(keySelector: contract => contract.Path)
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<string, string>>
        ReadBaselineComponentMarkupByNameAsync()
    {
        string[] packagePaths =
        [
            Path.Combine(
                path1: fixture.Settings.AssetsRoot,
                path2: "Packages",
                path3: "First Time Setup",
                path4: "first-app.json"),
            Path.Combine(
                path1: fixture.Settings.AssetsRoot,
                path2: "Packages",
                path3: "First Time Setup",
                path4: "common-cache.json")
        ];

        Dictionary<string, string> componentMarkupByName = new(
            comparer: StringComparer.OrdinalIgnoreCase);

        foreach (string path in packagePaths)
        {
            using JsonDocument package = JsonDocument.Parse(
                json: await File.ReadAllTextAsync(path: path));

            JsonElement componentItem = package.RootElement
                .GetProperty(propertyName: "Items")
                .EnumerateArray()
                .Single(predicate: item => string.Equals(
                    a: item.GetProperty(propertyName: "Type")
                        .GetString(),
                    b: "ContentManagement/Component",
                    comparisonType: StringComparison.Ordinal));

            using JsonDocument components = JsonDocument.Parse(
                json: componentItem.GetProperty(propertyName: "Data")
                    .GetString()!);

            foreach (JsonElement component in components.RootElement
                .EnumerateArray())
            {
                string name = component.GetProperty(propertyName: "Name")
                    .GetString() ?? string.Empty;

                string content = component.TryGetProperty(
                    propertyName: "Content",
                    value: out JsonElement contentElement)
                        ? contentElement.GetString() ?? string.Empty
                        : string.Empty;

                componentMarkupByName[name] = content;
            }
        }

        return componentMarkupByName;
    }

    private static BaselinePageContract CreatePageContract(
        JsonElement page,
        IReadOnlyDictionary<string, string> componentMarkupByName)
    {
        string path = page.GetProperty(propertyName: "Path")
            .GetString() ?? string.Empty;

        string markup = string.Join(
            separator: Environment.NewLine,
            values: page.GetProperty(propertyName: "Contents")
                .EnumerateArray()
                .Select(selector: content =>
                    content.GetProperty(propertyName: "Html")
                        .GetString() ?? string.Empty));

        string[] directComponents = GetComponentNames(markup: markup);

        HashSet<string> components = new(
            collection: directComponents,
            comparer: StringComparer.OrdinalIgnoreCase);

        Queue<string> pendingComponents = new(directComponents);

        while (pendingComponents.TryDequeue(result: out string? componentName))
        {
            if (!componentMarkupByName.TryGetValue(
                key: componentName,
                value: out string? componentMarkup))
            {
                continue;
            }

            foreach (string childComponent in GetComponentNames(
                markup: componentMarkup))
            {
                if (components.Add(item: childComponent))
                {
                    pendingComponents.Enqueue(item: childComponent);
                }
            }
        }

        return new BaselinePageContract(
            Path: path,
            Components: components.OrderBy(keySelector: name => name)
                .ToArray());
    }

    private static string[] GetComponentNames(string markup) =>
        Regex.Matches(
            input: markup,
            pattern: "\\[component\\[([^\\]]+)\\]\\]",
            options: RegexOptions.CultureInvariant)
            .Select(selector: match => match.Groups[groupnum: 1].Value)
            .Distinct(comparer: StringComparer.Ordinal)
            .ToArray();

    private static string BuildFailureMessage(IEnumerable<string> failures)
    {
        StringBuilder message = new(
            value: "First Time Setup page failures:");

        foreach (string failure in failures)
        {
            message.AppendLine();
            message.Append(value: failure);
        }

        return message.ToString();
    }

    private sealed record BaselinePageContract(
        string Path,
        string[] Components)
    {
        internal string PathDisplay
        {
            get
            {
                return string.IsNullOrWhiteSpace(value: Path)
                    ? "/"
                    : $"/{Path}";
            }
        }
    }
}