// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Exceptions;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class CreateCommandHandlingOrchestrationService
{
    private static async Task<T> TryCatch<T>(
        Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException innerException)
        {
            throw new PackerServiceValidationException(
                innerException: innerException);
        }
        catch (InvalidOperationException innerException)
        {
            throw new PackerServiceDependencyException(
                innerException: innerException);
        }
        catch (Exception innerException)
        {
            throw new PackerServiceException(
                innerException: innerException);
        }
    }
}