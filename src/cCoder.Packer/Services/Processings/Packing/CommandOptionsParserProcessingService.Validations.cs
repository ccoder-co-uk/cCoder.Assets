// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Validations;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class CommandOptionsParserProcessingService
{
    private static void Validate(params object?[] inputs)
    {
        ValidationRulesEngine.Validate(inputs: inputs);
    }
}