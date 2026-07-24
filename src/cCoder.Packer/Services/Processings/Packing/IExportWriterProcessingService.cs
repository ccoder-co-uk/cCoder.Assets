// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Exports;

namespace cCoder.Packer.Services.Processings.Packing;

internal interface IExportWriterProcessingService
{
    Task<IReadOnlyList<string>> WriteExportRecordsAsync(
        IEnumerable<ExportRecord> records,
        CancellationToken cancellationToken = default);
}