// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Data.SqlClient;

namespace cCoder.Assets.UI.Tests.Models;

internal sealed record PublishedCoreSettings(
    string PublishRoot,
    string CoreConnectionString,
    string SecurityConnectionString,
    string SecurityDecryptionKey,
    string AssetsRoot,
    string ArtifactsRoot)
{
    internal static PublishedCoreSettings Load()
    {
        string suffix = $"-assets-ui-acceptance-{Guid.NewGuid():N}";

        return new PublishedCoreSettings(
            PublishRoot: ReadRequired(name: "CCODER_CORE_PUBLISH_ROOT"),
            CoreConnectionString: AddDatabaseSuffix(
                connectionString: ReadRequired(name: "AppSecurity__ConnectionString"),
                suffix: suffix),
            SecurityConnectionString: AddDatabaseSuffix(
                connectionString: ReadRequired(name: "Security__ConnectionString"),
                suffix: suffix),
            SecurityDecryptionKey: ReadRequired(name: "Security__DecryptionKey"),
            AssetsRoot: FindRepositoryRoot(),
            ArtifactsRoot: Path.Combine(
                path1: Path.GetTempPath(),
                path2: "cCoder.Assets.UI.Tests",
                path3: Guid.NewGuid()
                    .ToString(format: "N")));
    }

    private static string AddDatabaseSuffix(
        string connectionString,
        string suffix)
    {
        SqlConnectionStringBuilder builder = new(connectionString)
        {
            Encrypt = true,
            TrustServerCertificate = true
        };

        if (string.IsNullOrWhiteSpace(value: builder.InitialCatalog))
        {
            throw new InvalidOperationException(
                "Acceptance connection strings must name a database.");
        }

        builder.InitialCatalog += suffix;
        return builder.ConnectionString;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(
                path: Path.Combine(
                    path1: directory.FullName,
                    path2: "Data"))
                && Directory.Exists(
                    path: Path.Combine(
                        path1: directory.FullName,
                        path2: "Packages")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "The cCoder.Assets repository root could not be found.");
    }

    private static string ReadRequired(string name) =>
        Environment.GetEnvironmentVariable(variable: name)
        ?? Environment.GetEnvironmentVariable(
            variable: name,
            target: EnvironmentVariableTarget.User)
        ?? Environment.GetEnvironmentVariable(
            variable: name,
            target: EnvironmentVariableTarget.Machine)
        ?? throw new InvalidOperationException(
            $"Required UI acceptance setting '{name}' was not found.");
}