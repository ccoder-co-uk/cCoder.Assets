// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal interface ICommandResolvingOrchestrationService
{
    Task<int> ResolveAsync(
        string[] args,
        CancellationToken cancellationToken = default);
}