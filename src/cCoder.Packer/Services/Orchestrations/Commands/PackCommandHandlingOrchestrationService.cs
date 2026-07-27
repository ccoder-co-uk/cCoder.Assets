// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class PackCommandHandlingOrchestrationService(
    IConfiguredPathProcessingService configuredPathService,
    IPackageBuilderProcessingService packageBuilderService)
    : IPackCommandHandlingOrchestrationService
{
    public Task<int> HandleCommandOptionsAsync(
        CommandOptions command,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [command, cancellationToken]);

            string dataPath = configuredPathService.ResolveDataPath(
                suppliedPath: command.DataPath);

            if (!string.IsNullOrWhiteSpace(value: command.DestinationPath))
            {
                string destinationPath = Path.GetFullPath(
                    path: command.DestinationPath);

                string file = await packageBuilderService.BuildPackageAsync(
                    sourcePath: dataPath,
                    destinationPath: destinationPath,
                    packageName: command.PackageName,
                    category: command.Category,
                    cancellationToken: cancellationToken);

                Console.WriteLine(
                    value: $"Built package '{file}' from '{dataPath}'.");

                return 0;
            }

            string packagesPath =
                configuredPathService.ResolvePackagesPath(
                    suppliedPath: command.PackagesPath);

            IReadOnlyList<string> files =
                await packageBuilderService.BuildPackagesAsync(
                    dataPath: dataPath,
                    packagesPath: packagesPath,
                    cancellationToken: cancellationToken);

            Console.WriteLine(
                value: $"Built {files.Count} package files in " +
                    $"'{packagesPath}'.");

            return 0;
        });
}