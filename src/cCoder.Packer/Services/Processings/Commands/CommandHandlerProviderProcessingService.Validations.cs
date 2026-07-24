// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Validations;

namespace cCoder.Packer.Services.Processings.Commands;

internal sealed partial class CommandHandlerProviderProcessingService
{
    private static void Validate(params object?[] inputs) =>
        ValidationRulesEngine.Validate(inputs: inputs);
}