// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Text.Json;
using cCoder.Packer.Models.Configurations;

namespace cCoder.Packer.Dependencies;

internal static class PackerSettingsLoaderDependency
{
    public static PackerSettings Load()
    {
        string settingsFile = FindSettingsFile();

        using FileStream stream = File.OpenRead(path: settingsFile);

        SettingsDocument settings = JsonSerializer.Deserialize<SettingsDocument>(
            utf8Json: stream,
            options: JsonDefaultsDependency.Options)
            ?? throw new InvalidOperationException(
                message: $"Could not read settings from '{settingsFile}'.");

        string basePath = Path.GetDirectoryName(path: settingsFile)!;

        return new PackerSettings(
            DataPath: Path.GetFullPath(
                path: settings.Packer.DataPath,
                basePath: basePath),
            PackagesPath: Path.GetFullPath(
                path: settings.Packer.PackagesPath,
                basePath: basePath));
    }

    private static string FindSettingsFile()
    {
        IEnumerable<string> roots =
        [
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        ];

        foreach (string root in roots)
        {
            DirectoryInfo? directory = new(path: root);

            while (directory is not null)
            {
                string repositorySettings = Path.Combine(
                    path1: directory.FullName,
                    path2: "src",
                    path3: "cCoder.Packer",
                    path4: "appsettings.json");

                if (File.Exists(path: repositorySettings))
                {
                    return repositorySettings;
                }

                string localSettings = Path.Combine(
                    path1: directory.FullName,
                    path2: "appsettings.json");

                if (File.Exists(path: localSettings))
                {
                    return localSettings;
                }

                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException(
            message: "Could not locate appsettings.json.");
    }

    private sealed record SettingsDocument(PackerSettings Packer);
}