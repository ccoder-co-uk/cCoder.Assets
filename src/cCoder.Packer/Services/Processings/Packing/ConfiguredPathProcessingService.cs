// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Configurations;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class ConfiguredPathProcessingService(
    PackerConfiguration configuration)
    : IConfiguredPathProcessingService
{
    public string ResolveDataPath(string? suppliedPath) =>
        TryCatch(operation: () =>
        {
            Validate(inputs: suppliedPath);

            return ResolvePath(
                suppliedPath: suppliedPath,
                configuredPath: configuration.DataPath,
                configurationName: "data");
        });

    public string ResolvePackagesPath(string? suppliedPath) =>
        TryCatch(operation: () =>
        {
            Validate(inputs: suppliedPath);

            return ResolvePath(
                suppliedPath: suppliedPath,
                configuredPath: configuration.PackagesPath,
                configurationName: "packages");
        });

    public string ResolveBaselinePath(string? suppliedPath) =>
        TryCatch(operation: () =>
        {
            Validate(inputs: suppliedPath);

            if (string.IsNullOrWhiteSpace(value: suppliedPath))
            {
                throw new ArgumentException(
                    message: "The baseline path is required.");
            }

            return Path.GetFullPath(
                path: suppliedPath,
                basePath: Environment.CurrentDirectory);
        });

    private static string ResolvePath(
        string? suppliedPath,
        string? configuredPath,
        string configurationName)
    {
        string requiredConfiguredPath =
            configuredPath
            ?? throw new InvalidOperationException(
                message: $"The configured {configurationName} path is required.");

        string path = suppliedPath ?? requiredConfiguredPath;

        return Path.GetFullPath(
            path: path,
            basePath: Environment.CurrentDirectory);
    }
}