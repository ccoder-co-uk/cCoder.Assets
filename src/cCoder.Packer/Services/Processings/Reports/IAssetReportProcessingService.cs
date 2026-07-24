// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Processings.Reports;

internal interface IAssetReportProcessingService
{
    Task<string> WriteAsync(
        string dataPath,
        CancellationToken cancellationToken = default);
}