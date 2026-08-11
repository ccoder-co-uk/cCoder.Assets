// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Net;
using System.Net.Sockets;
using cCoder.Assets.UI.Tests.Diagnostics;
using cCoder.Assets.UI.Tests.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Infrastructure;

public sealed class PublishedCoreFixture : IAsyncLifetime
{
    private readonly List<ExternalApplication> applications = [];
    private AcceptanceDatabaseScope? databaseScope;
    private IPlaywright? playwright;
    private IBrowser? browser;

    internal PublishedCoreSettings Settings { get; private set; } = null!;

    internal Uri WebBaseAddress { get; private set; } = null!;

    internal string ApplicationLogs => string.Join(
        separator: Environment.NewLine,
        values: applications.Select(
            selector: application => application.Output));

    public async Task InitializeAsync()
    {
        Settings = PublishedCoreSettings.Load();

        databaseScope = new AcceptanceDatabaseScope(
            Settings.CoreConnectionString,
            Settings.SecurityConnectionString);

        int hostedServicesPort = FindFreePort();
        int webPort = FindFreePort();

        WebBaseAddress = new Uri($"http://127.0.0.1:{webPort}/");

        Dictionary<string, string> environment = CreateEnvironment(
            hostedServicesPort: hostedServicesPort);

        await StartDotNetApplicationAsync(
            name: "HostedServices",
            port: hostedServicesPort,
            environment: environment);

        await StartDotNetApplicationAsync(
            name: "Web",
            port: webPort,
            environment: environment);

        playwright = await Playwright.CreateAsync();

        browser = await playwright.Chromium.LaunchAsync(
            options: new BrowserTypeLaunchOptions
            {
                Headless = true
            });

        await CompleteFirstTimeSetupAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (browser is not null)
            {
                await browser.DisposeAsync();
            }

            playwright?.Dispose();
        }
        finally
        {
            foreach (ExternalApplication application in applications.AsEnumerable()
                .Reverse())
            {
                await application.DisposeAsync();
            }

            SqlConnection.ClearAllPools();

            if (databaseScope is not null)
            {
                await databaseScope.DisposeAsync();
            }
        }
    }

    internal async Task<IPage> NewPageAsync()
    {
        if (browser is null)
        {
            throw new InvalidOperationException(
                "The published Core browser has not started.");
        }

        IBrowserContext context = await browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    internal Task ClosePageAsync(IPage page) =>
        page.Context.CloseAsync();

    private async Task CompleteFirstTimeSetupAsync()
    {
        IPage page = await NewPageAsync();
        BrowserDiagnosticCollector diagnostics = new();
        diagnostics.Attach(page: page);

        try
        {
            await page.GotoAsync(
                url: new Uri(WebBaseAddress, "Setup").ToString());

            await page.GetByLabel(text: "Display name")
                .FillAsync(value: "Assets Acceptance Admin");

            await page.GetByLabel(text: "Email address")
                .FillAsync(value: "assets-acceptance@localhost");

            await page.GetByLabel(
                text: "Password",
                options: new() { Exact = true })
                .FillAsync(value: "AssetsAcceptance123!");

            await page.GetByLabel(text: "Confirm password")
                .FillAsync(value: "AssetsAcceptance123!");

            await page.GetByRole(
                role: AriaRole.Button,
                options: new() { Name = "Submit(details);" })
                .ClickAsync();

            await WaitForSetupCompletionAsync(page: page);
            await WaitForImportedBaselineAsync();
        }
        catch (Exception exception)
        {
            string artifactDirectory = Path.Combine(
                path1: Settings.ArtifactsRoot,
                path2: "FirstTimeSetup");

            await diagnostics.WriteAsync(
                page: page,
                artifactDirectory: artifactDirectory,
                processLogs: ApplicationLogs);

            string setupLog = await ReadSetupLogAsync(page: page);

            throw new InvalidOperationException(
                $"First-time setup failed at '{page.Url}'. Artifacts: "
                + artifactDirectory
                + Environment.NewLine
                + setupLog,
                exception);
        }
        finally
        {
            await ClosePageAsync(page: page);
        }
    }

    private Dictionary<string, string> CreateEnvironment(
        int hostedServicesPort) =>
        new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Acceptance",
            ["AppSecurity__ConnectionString"] = Settings.CoreConnectionString,
            ["AppSecurity__AggregateDomains"] = "false",
            ["Security__ConnectionString"] = Settings.SecurityConnectionString,
            ["Security__DecryptionKey"] = Settings.SecurityDecryptionKey,
            ["ContentManagement__ConnectionString"] = Settings.CoreConnectionString,
            ["DocumentManagement__ConnectionString"] = Settings.CoreConnectionString,
            ["Logging__ConnectionString"] = Settings.CoreConnectionString,
            ["Mail__ConnectionString"] = Settings.CoreConnectionString,
            ["Packaging__ConnectionString"] = Settings.CoreConnectionString,
            ["Workflow__ConnectionString"] = Settings.CoreConnectionString,
            ["Eventing__ProviderType"] = "Http",
            ["Eventing__Http__HubUrl"] =
                $"http://127.0.0.1:{hostedServicesPort}/Api/Eventing",
            ["Eventing__Http__MaxConcurrency"] = "1"
        };

    private async Task WaitForSetupCompletionAsync(IPage page)
    {
        DateTime timeoutAt = DateTime.UtcNow.AddMinutes(value: 2);

        while (DateTime.UtcNow < timeoutAt)
        {
            Uri currentAddress = new(uriString: page.Url);

            if (string.Equals(
                a: currentAddress.GetLeftPart(
                    part: UriPartial.Authority),
                b: WebBaseAddress.GetLeftPart(
                    part: UriPartial.Authority),
                comparisonType: StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    a: currentAddress.AbsolutePath,
                    b: "/",
                    comparisonType: StringComparison.Ordinal))
            {
                return;
            }

            string setupLog = await ReadSetupLogAsync(page: page);

            if (setupLog.Contains(
                value: "Setup stopped",
                comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "First-time setup reported a terminal failure."
                    + Environment.NewLine
                    + setupLog);
            }

            await Task.Delay(millisecondsDelay: 250);
        }

        throw new TimeoutException(
            "First-time setup did not complete within two minutes.");
    }

    private async Task WaitForImportedBaselineAsync()
    {
        DateTime timeoutAt = DateTime.UtcNow.AddSeconds(value: 30);
        int baselineState = 0;

        while (DateTime.UtcNow < timeoutAt)
        {
            baselineState = await GetImportedBaselineStateAsync();

            if (baselineState == 63)
            {
                IPage anonymousPage = await NewPageAsync();

                try
                {
                    await anonymousPage.GotoAsync(
                        url: new Uri(WebBaseAddress, "Login").ToString());

                    await anonymousPage
                        .Locator(selector: ".component[name='Login']")
                        .WaitForAsync();
                }
                finally
                {
                    await ClosePageAsync(page: anonymousPage);
                }

                return;
            }

            await Task.Delay(millisecondsDelay: 250);
        }

        throw new TimeoutException(
            "The imported baseline did not become render-ready before timeout. "
            + $"State={baselineState} (LoginPage=1, LoginComponent=2, "
            + "RootGuestsPageRole=4, GuestsRole=8, EmptyPathPage=16, AnyPageRole=32).");
    }

    private async Task<int> GetImportedBaselineStateAsync()
    {
        await using SqlConnection connection = new(
            connectionString: Settings.CoreConnectionString);

        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM [CMS].[Pages]
                    WHERE [AppId] = 1
                        AND [Path] = 'Login')
                THEN 1 ELSE 0 END
                + CASE WHEN EXISTS (
                    SELECT 1
                    FROM [CMS].[Components]
                    WHERE [Name] = 'Login')
                THEN 2 ELSE 0 END
                + CASE WHEN EXISTS (
                    SELECT 1
                    FROM [CMS].[Pages] AS page
                        INNER JOIN [Security].[PageRoles] AS pageRole
                            ON pageRole.PageId = page.Id
                        INNER JOIN [Security].[Roles] AS role
                            ON role.Id = pageRole.RoleId
                        WHERE page.AppId = 1
                            AND page.Path = ''
                            AND role.Name = 'Guests')
                THEN 4 ELSE 0 END
                + CASE WHEN EXISTS (
                    SELECT 1
                    FROM [Security].[Roles]
                    WHERE [AppId] = 1
                        AND [Name] = 'Guests')
                THEN 8 ELSE 0 END
                + CASE WHEN EXISTS (
                    SELECT 1
                    FROM [CMS].[Pages]
                    WHERE [AppId] = 1
                        AND [Path] = '')
                THEN 16 ELSE 0 END
                + CASE WHEN EXISTS (
                    SELECT 1
                    FROM [Security].[PageRoles])
                THEN 32 ELSE 0 END;
            """;

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(value: result);
    }

    private static async Task<string> ReadSetupLogAsync(IPage page)
    {
        ILocator setupLog = page.Locator(selector: "#setup-log");

        if (await setupLog.CountAsync() == 0)
        {
            return string.Empty;
        }

        try
        {
            return await setupLog.TextContentAsync(
                options: new LocatorTextContentOptions
                {
                    Timeout = 1_000
                }) ?? string.Empty;
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }

    private async Task StartDotNetApplicationAsync(
        string name,
        int port,
        Dictionary<string, string> environment)
    {
        string applicationRoot = Path.Combine(
            path1: Settings.PublishRoot,
            path2: name);

        string assemblyPath = Path.Combine(
            path1: applicationRoot,
            path2: $"{name}.dll");

        if (!File.Exists(path: assemblyPath))
        {
            throw new FileNotFoundException(
                $"Published {name} application was not found.",
                assemblyPath);
        }

        Dictionary<string, string> processEnvironment = new(environment)
        {
            ["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}"
        };

        ExternalApplication application = new(name: name);
        applications.Add(item: application);

        await application.StartAsync(
            fileName: "dotnet",
            arguments: $"\"{assemblyPath}\"",
            workingDirectory: applicationRoot,
            environment: processEnvironment,
            readinessProbe: () => ProbeAsync(
                uri: new Uri($"http://127.0.0.1:{port}/Health")),
            timeout: TimeSpan.FromMinutes(minutes: 2));
    }

    private static async Task<bool> ProbeAsync(Uri uri)
    {
        try
        {
            using HttpClient client = new();

            using HttpResponseMessage response = await client.GetAsync(
                requestUri: uri);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static int FindFreePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}