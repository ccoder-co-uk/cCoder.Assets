// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Assets.UI.Tests.Diagnostics;
using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests.Components;

internal sealed partial class ComponentTestDriver(PublishedCoreFixture fixture)
{
    internal Task AssertComponentRendersAsync(
        string pagePath,
        string componentName,
        bool navigate = false) =>
        AssertAuthenticatedActionAsync(
            pagePath: pagePath,
            componentName: componentName,
            action: async page =>
            {
                if (navigate)
                {
                    await NavigateComponentTreeAsync(page: page);
                }

                await page.Locator(
                    selector: $".component[name='{componentName}']")
                    .WaitForAsync(
                        options: new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Attached,
                            Timeout = 10_000
                        });
            });

    internal async Task AssertAuthenticatedActionAsync(
        string pagePath,
        string componentName,
        Func<IPage, Task> action)
    {
        IPage page = await fixture.NewPageAsync();
        BrowserDiagnosticCollector diagnostics = new();

        try
        {
            if (pagePath is not "Login" and not "ResetPassword")
            {
                await LoginAsync(page: page);

                await page.WaitForLoadStateAsync(
                    state: LoadState.NetworkIdle);
            }

            diagnostics.Attach(page: page);

            string address = pagePath == "ResetPassword"
                ? "ResetPassword?token=preview&uid=AssetsAcceptanceAdmin"
                : pagePath;

            IResponse? response = await page.GotoAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: address).ToString());

            Assert.NotNull(@object: response);

            Assert.True(
                condition: response.Ok,
                userMessage: $"/{pagePath} returned {response.Status}.");

            await page.Locator(selector: "main.site-main")
                .WaitForAsync();

            await action(arg: page);

            await page.WaitForLoadStateAsync(
                state: LoadState.NetworkIdle);

            await AssertGridConventionsAsync(
                page: page,
                componentName: componentName);

            await AssertTreeConventionsAsync(
                page: page,
                componentName: componentName);

            await AssertVisibleDialogConventionsAsync(
                page: page,
                componentName: componentName);

            await Assertions.Expect(
                locator: page.Locator(
                    selector: ".k-notification-error:visible, "
                        + ".alert-danger:visible, "
                        + "[role='alert'].error:visible"))
                .ToHaveCountAsync(count: 0);

            string content = await page.ContentAsync();

            Assert.DoesNotContain(
                expectedSubstring: "[[Missing Component",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "[component[",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "[style[",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "[script[",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "The page could not be rendered.",
                actualString: content);

            Assert.DoesNotContain(
                expectedSubstring: "was not found. Available",
                actualString: content);

            diagnostics.ThrowIfBroken();
        }
        catch
        {
            await diagnostics.WriteAsync(
                page: page,
                artifactDirectory: Path.Combine(
                    path1: fixture.Settings.ArtifactsRoot,
                    path2: GetType().Name,
                    path3: componentName),
                processLogs: fixture.ApplicationLogs);

            throw;
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }

    internal static async Task AssertMonacoEditorAsync(
        IPage page,
        string containerSelector,
        string language)
    {
        ILocator editor = page.Locator(
            selector: $"{containerSelector} .monaco-editor");

        await editor.First.WaitForAsync(
            options: new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });

        bool hasLanguageModel = await page.EvaluateAsync<bool>(
            expression: "language => Boolean(window.monaco?.editor) "
                + "&& window.monaco.editor.getModels()"
                + ".some(model => (model.getLanguageId?.() "
                + "?? model.getModeId?.()) === language)",
            arg: language);

        Assert.True(
            condition: hasLanguageModel,
            userMessage: $"No Monaco model was initialized for '{language}'.");
    }

    internal static async Task AssertKendoWidgetAsync(
        IPage page,
        string selector,
        string widgetName)
    {
        await page.Locator(selector: selector).First.WaitForAsync();

        await page.WaitForFunctionAsync(
            expression: "args => Boolean(window.jQuery) "
                + "&& Boolean(window.jQuery(args.selector).first()"
                + ".data(args.widgetName))",
            arg: new { selector, widgetName },
            options: new PageWaitForFunctionOptions
            {
                Timeout = 15_000
            });

    }

    private static async Task NavigateComponentTreeAsync(IPage page)
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

        if (await expanders.CountAsync() > 0
            && await expanders.First.IsEnabledAsync())
        {
            await expanders.First.ClickAsync();
        }
    }

    private async Task LoginAsync(IPage page)
    {
        await page.GotoAsync(
            url: new Uri(fixture.WebBaseAddress, "Login").ToString());

        await page.GetByLabel(text: "User =")
            .FillAsync(value: "assets-acceptance@localhost");

        await page.GetByLabel(text: "Password =")
            .FillAsync(value: "AssetsAcceptance123!");

        await page.GetByRole(
            role: AriaRole.Button,
            options: new() { Name = "Submit(details);" })
            .ClickAsync();

        await page.WaitForURLAsync(
            url: url => new Uri(uriString: url).AbsolutePath == "/");
    }
}