// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Exceptions;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class CommandOptionsParserProcessingService
{
    private static T TryCatch<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (ArgumentException innerException)
        {
            throw new PackerServiceValidationException(innerException);
        }
        catch (InvalidOperationException innerException)
        {
            throw new PackerServiceDependencyException(innerException);
        }
        catch (Exception innerException)
        {
            throw new PackerServiceException(innerException);
        }
    }
}