// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Exceptions;

internal sealed class PackerServiceDependencyException(Exception innerException)
    : Exception(message: "A Packer service dependency failed.", innerException);