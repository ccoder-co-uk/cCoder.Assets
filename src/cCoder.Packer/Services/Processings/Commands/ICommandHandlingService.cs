// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Services.Processings.Commands;

internal interface ICommandHandlingService
{
    Task<int> HandleCommandOptionsAsync(
        CommandOptions command,
        CancellationToken cancellationToken = default);
}