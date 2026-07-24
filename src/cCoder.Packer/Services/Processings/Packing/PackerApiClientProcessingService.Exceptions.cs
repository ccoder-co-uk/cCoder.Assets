// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Exceptions;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class PackerApiClientProcessingService
{
    private static async Task TryCatch(Func<Task> operation)
    {
        try
        {
            await operation();
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

    private static async Task<T> TryCatch<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
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