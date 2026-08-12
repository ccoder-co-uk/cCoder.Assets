// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using cCoder.Assets.UI.Tests.Diagnostics;
using cCoder.Assets.UI.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace cCoder.Assets.UI.Tests.Tests;

[Collection(name: "Published Core UI")]
public sealed partial class BaselineUiTests(PublishedCoreFixture fixture)
{
    [Fact]
    public async Task FirstTimeSetup_ShouldProduceRenderableHomepage()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        BrowserDiagnosticCollector diagnostics = new();
        diagnostics.Attach(page: page);

        try
        {
            await AssertAnonymousBaselineAccessAsync();

            await WaitForPageRenderCacheCountAsync(expectedCount: 1);

            await DeletePageRenderCacheAsync();

            // When
            Stopwatch uncachedTimer = Stopwatch.StartNew();

            IAPIResponse uncachedResponse = await page.Context.APIRequest.GetAsync(
                url: fixture.WebBaseAddress.ToString());

            uncachedTimer.Stop();

            // Then
            Assert.True(condition: uncachedResponse.Ok);

            string content = await uncachedResponse.TextAsync();

            AssertRenderable(content: content);

            string uncachedRenderMessage =
                $"Uncached render took {uncachedTimer.Elapsed.TotalMilliseconds:F0} ms.";

            Assert.True(
                condition: uncachedTimer.Elapsed < TimeSpan.FromSeconds(value: 1.2),
                userMessage: uncachedRenderMessage);

            await WaitForPageRenderCacheCountAsync(expectedCount: 1);

            Stopwatch cachedTimer = Stopwatch.StartNew();

            IAPIResponse cachedResponse = await page.Context.APIRequest.GetAsync(
                url: fixture.WebBaseAddress.ToString());

            cachedTimer.Stop();

            Assert.True(condition: cachedResponse.Ok);

            Assert.True(
                condition: cachedTimer.Elapsed < TimeSpan.FromMilliseconds(value: 750),
                userMessage: $"Cached render took {cachedTimer.Elapsed.TotalMilliseconds:F0} ms.");

            string cachedContent = await cachedResponse.TextAsync();

            string comparisonArtifacts = Path.Combine(
                path1: fixture.Settings.ArtifactsRoot,
                path2: "FirstTimeSetupRenderComparison");

            Directory.CreateDirectory(path: comparisonArtifacts);

            await File.WriteAllTextAsync(
                path: Path.Combine(
                    path1: comparisonArtifacts,
                    path2: "uncached.html"),
                contents: content);

            await File.WriteAllTextAsync(
                path: Path.Combine(
                    path1: comparisonArtifacts,
                    path2: "cached.html"),
                contents: cachedContent);

            Assert.Equal(
                expected: NormalizeRequestValues(content: content),
                actual: NormalizeRequestValues(content: cachedContent));

            IResponse? browserResponse = await page.GotoAsync(
                url: fixture.WebBaseAddress.ToString());

            Assert.NotNull(@object: browserResponse);
            Assert.True(condition: browserResponse.Ok);

            await page.Locator(selector: "main.site-main")
                .WaitForAsync();

            diagnostics.ThrowIfBroken();
        }
        catch
        {
            await diagnostics.WriteAsync(
                page: page,
                artifactDirectory: Path.Combine(
                    path1: fixture.Settings.ArtifactsRoot,
                    path2: nameof(FirstTimeSetup_ShouldProduceRenderableHomepage)),
                processLogs: fixture.ApplicationLogs);

            throw;
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }

    [Fact]
    public async Task DefaultAppPackage_ShouldProduceRenderableHomepage()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        BrowserDiagnosticCollector diagnostics = new();
        diagnostics.Attach(page: page);

        try
        {
            await LoginAsInitialAdministratorAsync(page: page);

            IAPIResponse existingAppResponse = await page.Context.APIRequest.GetAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "Api/ContentManagement/App(1)?$select=TenantId")
                    .ToString());

            Assert.True(condition: existingAppResponse.Ok);

            using JsonDocument existingAppDocument = JsonDocument.Parse(
                json: await existingAppResponse.TextAsync());

            string tenantId = existingAppDocument.RootElement
                .GetProperty(propertyName: "TenantId")
                .GetString()!;

            JsonElement appResponse = await page.EvaluateAsync<JsonElement>(
                expression: """
                    async payload => {
                        const response = await fetch(
                            "/Api/ContentManagement/App",
                            {
                                method: "POST",
                                credentials: "same-origin",
                                headers: { "Content-Type": "application/json" },
                                body: JSON.stringify(payload)
                            });

                        return {
                            ok: response.ok,
                            status: response.status,
                            body: await response.text()
                        };
                    }
                    """,
                arg: new
                {
                    Name = "Assets Default App",
                    Domain = "localhost",
                    TenantId = tenantId
                });

            bool appResponseOk = appResponse
                .GetProperty(propertyName: "ok")
                .GetBoolean();

            int appResponseStatus = appResponse
                .GetProperty(propertyName: "status")
                .GetInt32();

            string appResponseBody = appResponse
                .GetProperty(propertyName: "body")
                .GetString()!;

            Assert.True(
                condition: appResponseOk,
                userMessage: $"App creation returned {appResponseStatus}: "
                    + appResponseBody
                    + Environment.NewLine
                    + fixture.ApplicationLogs);

            using JsonDocument appDocument = JsonDocument.Parse(
                json: appResponseBody);

            int appId = appDocument.RootElement.GetProperty(propertyName: "Id")
                .GetInt32();

            string packageJson = await File.ReadAllTextAsync(
                path: Path.Combine(
                    path1: fixture.Settings.AssetsRoot,
                    path2: "Packages",
                    path3: "Baseline New App",
                    path4: "baseline-new-app.json"));

            JsonElement importResponse = await page.EvaluateAsync<JsonElement>(
                expression: """
                    async ({ appId, packageJson }) => {
                        const response = await fetch(
                            `/Api/Packaging/Package/Import?appId=${appId}`,
                            {
                                method: "POST",
                                credentials: "same-origin",
                                headers: { "Content-Type": "application/json" },
                                body: packageJson
                            });

                        return {
                            ok: response.ok,
                            status: response.status,
                            body: await response.text()
                        };
                    }
                    """,
                arg: new
                {
                    appId = appId,
                    packageJson = packageJson
                });

            JsonElement importOk = importResponse.GetProperty(propertyName: "ok");
            bool importSucceeded = importOk.GetBoolean();

            JsonElement importStatus = importResponse.GetProperty(propertyName: "status");

            JsonElement importBodyElement = importResponse.GetProperty(propertyName: "body");
            string? importBody = importBodyElement.GetString();

            Assert.True(
                condition: importSucceeded,
                userMessage: $"Default app import returned "
                    + $"{importStatus}: {importBody}");

            await WaitForDefaultAppBaselineAsync(appId: appId);

            await WaitForPageRenderCacheCountAsync(expectedCount: 0, appId: appId);

            UriBuilder defaultAppAddressBuilder = new()
            {
                Scheme = fixture.WebBaseAddress.Scheme,
                Host = "localhost",
                Port = fixture.WebBaseAddress.Port
            };

            Uri defaultAppAddress = defaultAppAddressBuilder.Uri;
            string defaultAppAddressText = defaultAppAddress.ToString();

            // When
            Stopwatch uncachedTimer = Stopwatch.StartNew();

            IAPIResponse uncachedResponse = await page.Context.APIRequest.GetAsync(
                url: defaultAppAddressText);

            uncachedTimer.Stop();

            // Then
            string uncachedContent = await uncachedResponse.TextAsync();

            Assert.True(
                condition: uncachedResponse.Ok,
                userMessage: $"Default app render returned "
                    + $"{uncachedResponse.Status}: {uncachedContent}");

            AssertRenderable(content: uncachedContent);

            Assert.True(
                condition: uncachedTimer.Elapsed < TimeSpan.FromSeconds(value: 1.25),
                userMessage: $"Default app uncached render took {uncachedTimer.Elapsed.TotalMilliseconds:F0} ms.");

            await WaitForPageRenderCacheCountAsync(expectedCount: 1, appId: appId);

            Stopwatch cachedTimer = Stopwatch.StartNew();

            IAPIResponse cachedResponse = await page.Context.APIRequest.GetAsync(
                url: defaultAppAddress.ToString());

            cachedTimer.Stop();

            Assert.True(condition: cachedResponse.Ok);
            string cachedContent = await cachedResponse.TextAsync();
            AssertRenderable(content: cachedContent);

            Assert.True(
                condition: cachedTimer.Elapsed < TimeSpan.FromMilliseconds(value: 750),
                userMessage: $"Default app cached render took {cachedTimer.Elapsed.TotalMilliseconds:F0} ms.");

            string comparisonArtifacts = fixture.Settings.ArtifactsRoot
                + Path.DirectorySeparatorChar
                + nameof(DefaultAppPackage_ShouldProduceRenderableHomepage);

            string uncachedArtifactPath = Path.Combine(
                path1: comparisonArtifacts,
                path2: "uncached.html");

            string cachedArtifactPath = Path.Combine(
                path1: comparisonArtifacts,
                path2: "cached.html");

            Directory.CreateDirectory(path: comparisonArtifacts);

            await File.WriteAllTextAsync(
                path: uncachedArtifactPath,
                contents: uncachedContent);

            await File.WriteAllTextAsync(
                path: cachedArtifactPath,
                contents: cachedContent);

            Assert.Equal(
                expected: NormalizeRequestValues(content: uncachedContent),
                actual: NormalizeRequestValues(content: cachedContent));


            IResponse? browserResponse = await page.GotoAsync(
                url: defaultAppAddress.ToString());

            Assert.NotNull(@object: browserResponse);
            Assert.True(condition: browserResponse.Ok);

            await page.Locator(selector: "main.site-main")
                .WaitForAsync();

            diagnostics.ThrowIfBroken();
        }
        catch
        {
            await diagnostics.WriteAsync(
                page: page,
                artifactDirectory: Path.Combine(
                    path1: fixture.Settings.ArtifactsRoot,
                    path2: nameof(DefaultAppPackage_ShouldProduceRenderableHomepage)),
                processLogs: fixture.ApplicationLogs);

            throw;
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }

    private async Task LoginAsInitialAdministratorAsync(IPage page)
    {
        await page.GotoAsync(
            url: new Uri(
                baseUri: fixture.WebBaseAddress,
                relativeUri: "Login")
                .ToString());

        await page.GetByLabel(text: "User =")
            .FillAsync(value: "assets-acceptance@localhost");

        await page.GetByLabel(text: "Password =")
            .FillAsync(value: "AssetsAcceptance123!");

        await page.WaitForFunctionAsync(
            expression: "() => Boolean(window.Login) "
                + "&& Boolean(window.jQuery?._data("
                + "document.querySelector(\"button[name=login]\"), \"events\")?.click)");

        Task navigation = page.WaitForURLAsync(
            url: url => new Uri(uriString: url).AbsolutePath == "/");

        await page.GetByRole(
                role: AriaRole.Button,
                options: new() { Name = "Submit(details);" })
            .ClickAsync();

        Task completed = await Task.WhenAny(
            task1: navigation,
            task2: Task.Delay(millisecondsDelay: 10_000));

        if (completed != navigation)
        {
            string pageText = await page.Locator(selector: "body")
                .InnerTextAsync();

            throw new InvalidOperationException(
                $"Login remained at '{page.Url}'."
                + Environment.NewLine
                + pageText
                + Environment.NewLine
                + fixture.ApplicationLogs);
        }

        await navigation;
    }

    private static void AssertRenderable(string content)
    {
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
    }

    private async Task AssertAnonymousBaselineAccessAsync()
    {
        await using SqlConnection connection = new(
            connectionString: fixture.Settings.CoreConnectionString);

        await connection.OpenAsync();

        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                (SELECT STRING_AGG(
                    CONCAT(page.Id, ':', COALESCE(NULLIF(page.Path, ''), '<root>')),
                    ', ')
                FROM [CMS].[Pages] AS page
                WHERE page.AppId = 1),
                STRING_AGG(
                    CONCAT(page.Id, ':', COALESCE(NULLIF(page.Path, ''), '<root>')),
                    ', '),
                (SELECT COUNT(*)
                FROM [CMS].[PageRenderCache] AS cache
                INNER JOIN [CMS].[Pages] AS cachedPage
                    ON cachedPage.Id = cache.PageId
                WHERE cache.AppId = 1
                    AND cachedPage.Path = '')
            FROM [CMS].[Pages] AS page
            INNER JOIN [Security].[PageRoles] AS pageRole
                ON pageRole.PageId = page.Id
            INNER JOIN [Security].[Roles] AS role
                ON role.Id = pageRole.RoleId
            INNER JOIN [Security].[UserRoles] AS userRole
                ON userRole.RoleId = role.Id
            WHERE page.AppId = 1
                AND page.Path IN ('', 'Login')
                AND role.Name = 'Guests'
                AND userRole.UserId = 'Guest';
            """;

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        _ = await reader.ReadAsync();

        string allPages = reader.IsDBNull(i: 0)
            ? string.Empty
            : reader.GetString(i: 0);

        string guestPages = reader.IsDBNull(i: 1)
            ? string.Empty
            : reader.GetString(i: 1);

        int pageRenderCacheCount = reader.GetInt32(i: 2);

        Assert.True(
            condition: guestPages.Split(
                separator: ',',
                options: StringSplitOptions.RemoveEmptyEntries).Length == 2,
            userMessage: $"Pages: {allPages}. Guest pages: {guestPages}.");

        Assert.InRange(actual: pageRenderCacheCount, low: 0, high: 1);
    }

    private async Task DeletePageRenderCacheAsync()
    {
        await using SqlConnection connection = new(
            connectionString: fixture.Settings.CoreConnectionString);

        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = """
            DELETE cache
            FROM [CMS].[PageRenderCache] AS cache
            INNER JOIN [CMS].[Pages] AS page
                ON page.Id = cache.PageId
            WHERE cache.AppId = 1
                AND page.Path = '';
            """;

        _ = await command.ExecuteNonQueryAsync();
    }

    private async Task<int> GetPageRenderCacheCountAsync(int appId = 1)
    {
        await using SqlConnection connection = new(
            connectionString: fixture.Settings.CoreConnectionString);

        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM [CMS].[PageRenderCache] AS cache
            INNER JOIN [CMS].[Pages] AS page
                ON page.Id = cache.PageId
            WHERE cache.AppId = @appId
                AND page.Path = '';
            """;

        _ = command.Parameters.AddWithValue(parameterName: "@appId", value: appId);

        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }

    private async Task WaitForPageRenderCacheCountAsync(
        int expectedCount,
        int appId = 1)
    {
        DateTime timeoutAt = DateTime.UtcNow.AddSeconds(value: 30);

        while (DateTime.UtcNow < timeoutAt)
        {
            if (await GetPageRenderCacheCountAsync(appId: appId) == expectedCount)
            {
                return;
            }

            await Task.Delay(millisecondsDelay: 100);
        }

        int actualCount = await GetPageRenderCacheCountAsync(appId: appId);

        Assert.Fail(
            message: $"Expected {expectedCount} page render cache row(s), but found {actualCount} after 30 seconds.");
    }

    private async Task WaitForDefaultAppBaselineAsync(int appId)
    {
        DateTimeOffset timeout = DateTimeOffset.UtcNow.AddSeconds(seconds: 30);
        string observedState = string.Empty;

        while (DateTimeOffset.UtcNow < timeout)
        {
            await using SqlConnection connection = new(
                connectionString: fixture.Settings.CoreConnectionString);

            await connection.OpenAsync();

            await using SqlCommand command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM [CMS].[Pages]
                        WHERE AppId = @appId AND Path = '')
                        THEN 1 ELSE 0 END,
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM [CMS].[Layouts]
                        WHERE AppId = @appId AND Name = 'Article')
                        THEN 1 ELSE 0 END,
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM [CMS].[Pages] AS page
                        INNER JOIN [Security].[PageRoles] AS pageRole
                            ON pageRole.PageId = page.Id
                        INNER JOIN [Security].[Roles] AS role
                            ON role.Id = pageRole.RoleId
                        WHERE page.AppId = @appId
                            AND page.Path = ''
                            AND role.Name = 'Guests')
                        THEN 1 ELSE 0 END,
                    (SELECT COUNT(*)
                    FROM [CMS].[Pages]
                    WHERE AppId = @appId),
                    (SELECT COUNT(*)
                    FROM [CMS].[Layouts]
                    WHERE AppId = @appId);
                """;

            command.Parameters.AddWithValue(
                parameterName: "@appId",
                value: appId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            _ = await reader.ReadAsync();

            bool hasRoot = reader.GetInt32(i: 0) == 1;
            bool hasArticle = reader.GetInt32(i: 1) == 1;
            bool hasGuestAccess = reader.GetInt32(i: 2) == 1;
            int pageCount = reader.GetInt32(i: 3);
            int layoutCount = reader.GetInt32(i: 4);

            observedState = $"root={hasRoot}, article={hasArticle}, "
                + $"guestAccess={hasGuestAccess}, pages={pageCount}, "
                + $"layouts={layoutCount}";

            if (hasRoot
                && hasArticle
                && hasGuestAccess
                && pageCount == 11
                && layoutCount == 3)
            {
                return;
            }

            await Task.Delay(millisecondsDelay: 100);
        }

        Assert.Fail(
            message: "The default-app package did not finish importing "
                + "its root page, Article layout, and Guest access within "
                + $"30 seconds. Last state: {observedState}.");
    }

    private static string NormalizeRequestValues(string content) =>
        System.Text.RegularExpressions.Regex.Replace(
            input: content,
            pattern: "nonce=(['\"])[^'\"]+\\1",
            replacement: "nonce=$1[request[nonce]]$1",
            options: System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    [Fact]
    public async Task Login_ShouldNotRedirectWhenCredentialsAreRejected()
    {
        // Given
        IPage page = await fixture.NewPageAsync();
        BrowserDiagnosticCollector diagnostics = new();
        diagnostics.Attach(page: page);

        try
        {
            await page.GotoAsync(
                url: new Uri(
                    baseUri: fixture.WebBaseAddress,
                    relativeUri: "Login")
                    .ToString());

            await page.GetByLabel(text: "User =")
                .FillAsync(value: "Assets Acceptance Admin");

            await page.GetByLabel(text: "Password =")
                .FillAsync(value: "incorrect-password");

            // When
            await page.GetByRole(
                role: AriaRole.Button,
                options: new() { Name = "Submit(details);" })
                .ClickAsync();

            await page.WaitForTimeoutAsync(timeout: 500);

            // Then
            Assert.Equal(
                expected: "/Login",
                actual: new Uri(uriString: page.Url).AbsolutePath);
        }
        catch
        {
            await diagnostics.WriteAsync(
                page: page,
                artifactDirectory: Path.Combine(
                    path1: fixture.Settings.ArtifactsRoot,
                    path2: nameof(Login_ShouldNotRedirectWhenCredentialsAreRejected)),
                processLogs: fixture.ApplicationLogs);

            throw;
        }
        finally
        {
            await fixture.ClosePageAsync(page: page);
        }
    }
}