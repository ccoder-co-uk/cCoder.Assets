// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Validations;

namespace cCoder.Packer.Services.Processings.Reports;

internal sealed partial class AssetReportProcessingService
{
    private static void Validate(params object?[] inputs) =>
        ValidationRulesEngine.Validate(inputs: inputs);
}