// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace cCoder.Packer.Services.Processings.Commands;

internal sealed partial class CommandHandlerProviderProcessingService(
    IServiceProvider serviceProvider)
    : ICommandHandlerProviderProcessingService
{
    public ICommandHandlingService GetCommandHandlingService(
        string commandName) =>
        TryCatch(operation: () =>
        {
            Validate(inputs: commandName);

            ICommandHandlingService commandHandlingService = serviceProvider
                .GetRequiredKeyedService<ICommandHandlingService>(
                    serviceKey: commandName);

            Validate(inputs: commandHandlingService);

            return commandHandlingService;
        });
}