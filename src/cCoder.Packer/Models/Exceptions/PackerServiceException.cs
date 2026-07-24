// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Exceptions;

internal sealed class PackerServiceException(Exception innerException)
    : Exception(message: "A Packer service failed.", innerException);