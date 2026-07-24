// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Processings.Commands;

internal interface ICommandHandlerProviderProcessingService
{
    ICommandHandlingService GetCommandHandlingService(string commandName);
}