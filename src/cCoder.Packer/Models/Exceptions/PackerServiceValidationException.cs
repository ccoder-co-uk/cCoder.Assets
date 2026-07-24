// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Exceptions;

internal sealed class PackerServiceValidationException(Exception innerException)
    : Exception(message: "Packer service validation failed.", innerException);