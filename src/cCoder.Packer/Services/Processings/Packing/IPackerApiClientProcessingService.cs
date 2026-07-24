// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Exports;

namespace cCoder.Packer.Services.Processings.Packing;

internal interface IPackerApiClientProcessingService
{
    Task LoginAsync(string user, string password, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportRecord>> ExportCommonCacheAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportRecord>> ExportAppAsync(
        int? requestedAppId,
        CancellationToken cancellationToken = default);
}