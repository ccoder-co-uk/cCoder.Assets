// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text;
using Microsoft.Playwright;

namespace cCoder.Assets.UI.Tests.Diagnostics;

internal sealed class BrowserDiagnosticCollector
{
    private readonly List<string> consoleEntries = [];
    private readonly List<string> pageErrors = [];
    private readonly List<string> failedRequests = [];
    private readonly List<string> failedResponses = [];

    internal void Attach(IPage page)
    {
        page.Console += (_, message) =>
            consoleEntries.Add(item: $"{message.Type}: {message.Text}");

        page.PageError += (_, message) =>
            pageErrors.Add(item: message);

        page.RequestFailed += (_, request) =>
            failedRequests.Add(
                item: $"{request.Method} {request.Url}: "
                    + request.Failure);

        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                failedResponses.Add(
                    item: $"{response.Status} {response.Url}");
            }
        };
    }

    internal async Task WriteAsync(
        IPage page,
        string artifactDirectory,
        string processLogs)
    {
        Directory.CreateDirectory(path: artifactDirectory);

        await page.ScreenshotAsync(
            options: new PageScreenshotOptions
            {
                FullPage = true,
                Path = Path.Combine(
                    path1: artifactDirectory,
                    path2: "page.png")
            });

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: artifactDirectory,
                path2: "page.html"),
            contents: await page.ContentAsync());

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: artifactDirectory,
                path2: "browser.log"),
            contents: BuildReport());

        await File.WriteAllTextAsync(
            path: Path.Combine(
                path1: artifactDirectory,
                path2: "applications.log"),
            contents: processLogs);
    }

    internal string BuildReport()
    {
        StringBuilder report = new();

        Append(
            report: report,
            heading: "Console",
            entries: consoleEntries);

        Append(
            report: report,
            heading: "Page errors",
            entries: pageErrors);

        Append(
            report: report,
            heading: "Failed requests",
            entries: failedRequests);

        Append(
            report: report,
            heading: "Failed responses",
            entries: failedResponses);

        return report.ToString();
    }

    internal void Reset()
    {
        consoleEntries.Clear();
        pageErrors.Clear();
        failedRequests.Clear();
        failedResponses.Clear();
    }

    internal void ThrowIfBroken()
    {
        if (pageErrors.Count == 0
            && failedRequests.Count == 0
            && failedResponses.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(BuildReport());
    }

    private static void Append(
        StringBuilder report,
        string heading,
        IEnumerable<string> entries)
    {
        report.AppendLine(value: heading);

        foreach (string entry in entries)
        {
            report.AppendLine(value: entry);
        }

        report.AppendLine();
    }
}