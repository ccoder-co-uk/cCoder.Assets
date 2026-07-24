// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Validations;

internal static class ValidationRulesEngine
{
    public static void Validate(params object?[] inputs)
    {
        ArgumentNullException.ThrowIfNull(argument: inputs);

    }
}