// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;
using cCoder.Packer.Services.Processings.Reports;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class ReportCommandHandlingOrchestrationService(
    IAssetReportProcessingService assetReportService,
    IConfiguredPathProcessingService configuredPathService)
    : IReportCommandHandlingOrchestrationService
{
    public Task<int> HandleCommandOptionsAsync(
        CommandOptions command,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [command, cancellationToken]);

            string dataPath = configuredPathService.ResolveDataPath(
                suppliedPath: command.DataPath);

            string reportPath = await assetReportService.WriteAsync(
                dataPath: dataPath,
                cancellationToken: cancellationToken);

            Console.WriteLine(value: $"Report written to '{reportPath}'.");

            return 0;
        });

}