// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using cCoder.Packer.Brokers;
using cCoder.Packer.Models.Configurations;
using cCoder.Packer.Models.Packages;
using cCoder.Packer.Services.Processings.Provisioning;

namespace cCoder.Packer.Tests;

public sealed partial class CreateAppProcessingServiceTests
{
    [Fact]
    public async Task ShouldPassCreateAndImportCallsToApiBroker()
    {
        // Given
        string baselinePath = Path.Combine(
            path1: Path.GetTempPath(),
            path2: $"ccoder-create-{Guid.NewGuid():N}");

        string appPath = Path.Combine(
            path1: baselinePath,
            path2: "App");

        string commonCachePath = Path.Combine(
            path1: baselinePath,
            path2: "Common Cache");

        Directory.CreateDirectory(path: appPath);
        Directory.CreateDirectory(path: commonCachePath);

        await WritePackageAsync(
            path: Path.Combine(
                path1: appPath,
                path2: "Pages.json"),
            package: new AssetPackage(
                Name: "Pages",
                Description: "Test pages",
                Category: "ContentManagement",
                SourceApi: "ContentManagement",
                Items:
                [
                    new AssetPackageItem(
                        Type: "ContentManagement/Page",
                        Data: "[{\"Name\":\"Home\",\"Path\":\"\"}]"),
                ]));

        await WritePackageAsync(
            path: Path.Combine(
                path1: commonCachePath,
                path2: "Components.json"),
            package: new AssetPackage(
                Name: "Components",
                Description: "Test components",
                Category: "ContentManagement",
                SourceApi: "ContentManagement",
                Items:
                [
                    new AssetPackageItem(
                        Type: "ContentManagement/Component",
                        Data:
                            "[{\"Name\":\"Navigation\"," +
                            "\"ResourceKey\":\"ContentManagement\"}]"),
                ]));

        RecordingPackerApiBroker broker = new();

        CreateAppProcessingService service = new(
            apiBroker: broker);

        try
        {
            // When
            int appId = await service.ProvisionAppAsync(
                api: new Uri(uriString: "https://example.test/"),
                name: "Sample",
                tenantId: "tenant-one",
                user: "test-user",
                password: "test-password",
                baselinePath: baselinePath);

            // Then
            Assert.Equal(expected: 42, actual: appId);

            Assert.Equal(expected: 4, actual: broker.Calls.Count);

            AssertCall(
                call: broker.Calls[0],
                expectedUri:
                    "https://example.test/Api/Account/Login",
                expectedToken: null);

            Assert.Equal(
                expected: "test-user",
                actual: broker.Calls[0].Content
                    .GetProperty(propertyName: "User")
                    .GetString());

            Assert.Equal(
                expected: "test-password",
                actual: broker.Calls[0].Content
                    .GetProperty(propertyName: "Pass")
                    .GetString());

            AssertCall(
                call: broker.Calls[1],
                expectedUri:
                    "https://example.test/Api/ContentManagement/App",
                expectedToken: "test-token");

            Assert.Equal(
                expected: "tenant-one",
                actual: broker.Calls[1].Content
                    .GetProperty(propertyName: "TenantId")
                    .GetString());

            Assert.Equal(
                expected: "sample.example.test",
                actual: broker.Calls[1].Content
                    .GetProperty(propertyName: "Domain")
                    .GetString());

            Assert.Equal(
                expected: "Sample",
                actual: broker.Calls[1].Content
                    .GetProperty(propertyName: "Name")
                    .GetString());

            AssertCall(
                call: broker.Calls[2],
                expectedUri:
                    "https://example.test/Api/Core/Package/Import?appId=42",
                expectedToken: "test-token");

            Assert.Equal(
                expected: "Pages",
                actual: broker.Calls[2].Content
                    .GetProperty(propertyName: "Name")
                    .GetString());

            AssertCall(
                call: broker.Calls[3],
                expectedUri:
                    "https://example.test/Api/ContentManagement/" +
                        "CommonObject",
                expectedToken: "test-token");

            JsonElement commonObject = broker.Calls[3].Content[0];

            Assert.Equal(
                expected: "Navigation",
                actual: commonObject
                    .GetProperty(propertyName: "Name")
                    .GetString());

            Assert.Equal(
                expected: "ContentManagement/Component",
                actual: commonObject
                    .GetProperty(propertyName: "Type")
                    .GetString());

            Assert.Contains(
                expectedSubstring: "\"Name\":\"Navigation\"",
                actualString: commonObject
                    .GetProperty(propertyName: "Json")
                    .GetString());
        }
        finally
        {
            Directory.Delete(
                path: baselinePath,
                recursive: true);
        }
    }

    private static Task WritePackageAsync(
        string path,
        AssetPackage package) =>
        File.WriteAllTextAsync(
            path: path,
            contents: JsonSerializer.Serialize(
                value: package,
                options: JsonDefaults.Options));

    private static void AssertCall(
        BrokerCall call,
        string expectedUri,
        string? expectedToken)
    {
        Assert.Equal(
            expected: new Uri(uriString: expectedUri),
            actual: call.RequestUri);

        Assert.Equal(
            expected: expectedToken,
            actual: call.BearerToken);
    }

    private sealed class RecordingPackerApiBroker
        : IPackerApiBroker
    {
        public List<BrokerCall> Calls { get; } = [];

        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            using JsonDocument content = JsonDocument.Parse(
                json: await request.Content!.ReadAsStringAsync(
                    cancellationToken: cancellationToken));

            Calls.Add(item: new BrokerCall(
                RequestUri: request.RequestUri!,
                Content: content.RootElement.Clone(),
                BearerToken:
                    request.Headers.Authorization?.Parameter));

            object response = request.RequestUri!.AbsolutePath.EndsWith(
                value: "/Login",
                comparisonType: StringComparison.Ordinal)
                ? new { id = "test-token" }
                : request.RequestUri.AbsolutePath.EndsWith(
                    value: "/App",
                    comparisonType: StringComparison.Ordinal)
                    ? new { Id = 42 }
                    : new { };

            return new HttpResponseMessage(
                statusCode: HttpStatusCode.OK)
            {
                Content = JsonContent.Create(inputValue: response),
            };
        }
    }

    private sealed record BrokerCall(
        Uri RequestUri,
        JsonElement Content,
        string? BearerToken);
}