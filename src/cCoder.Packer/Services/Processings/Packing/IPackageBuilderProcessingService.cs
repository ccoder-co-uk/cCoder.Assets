// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Processings.Packing;

internal interface IPackageBuilderProcessingService
{
    Task<IReadOnlyList<string>> BuildPackagesAsync(
        string dataPath,
        string packagesPath,
        CancellationToken cancellationToken = default);
}