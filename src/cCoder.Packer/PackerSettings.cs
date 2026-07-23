using System.Text.Json;

namespace cCoder.Packer.Models;

public sealed record PackerSettings(string DataPath, string PackagesPath)
{
    public static PackerSettings Load()
    {
        string settingsFile = FindSettingsFile();
        using FileStream stream = File.OpenRead(settingsFile);
        SettingsDocument settings = JsonSerializer.Deserialize<SettingsDocument>(
            stream,
            JsonDefaults.Options)
            ?? throw new InvalidOperationException(
                $"Could not read settings from '{settingsFile}'.");

        string basePath = Path.GetDirectoryName(settingsFile)!;
        return new PackerSettings(
            Path.GetFullPath(settings.Packer.DataPath, basePath),
            Path.GetFullPath(settings.Packer.PackagesPath, basePath));
    }

    private static string FindSettingsFile()
    {
        IEnumerable<string> roots = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        };

        foreach (string root in roots)
        {
            DirectoryInfo? directory = new(root);
            while (directory is not null)
            {
                string repositorySettings = Path.Combine(
                    directory.FullName,
                    "src",
                    "cCoder.Packer",
                    "appsettings.json");

                if (File.Exists(repositorySettings))
                    return repositorySettings;

                string localSettings = Path.Combine(
                    directory.FullName,
                    "appsettings.json");

                if (File.Exists(localSettings))
                    return localSettings;

                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate appsettings.json.");
    }

    private sealed record SettingsDocument(PackerSettings Packer);
}
