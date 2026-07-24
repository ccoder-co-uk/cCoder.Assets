// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Validations;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class ReportCommandHandlingOrchestrationService
{
    private static void Validate(params object?[] inputs) =>
        ValidationRulesEngine.Validate(inputs: inputs);
}