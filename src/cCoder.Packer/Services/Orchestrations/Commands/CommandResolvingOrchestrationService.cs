// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;
using cCoder.Packer.Services.Processings.Commands;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class CommandResolvingOrchestrationService(
    ICommandOptionsParserProcessingService commandOptionsParser,
    ICommandHandlerProviderProcessingService commandHandlerProvider)
    : ICommandResolvingOrchestrationService
{
    public Task<int> ResolveAsync(
        string[] args,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [args, cancellationToken]);

            CommandOptions command = commandOptionsParser.Parse(args: args);

            ICommandHandlingService handler =
                commandHandlerProvider.GetCommandHandlingService(
                    commandName: command.Name);

            return await handler.HandleCommandOptionsAsync(
                command: command,
                cancellationToken: cancellationToken);
        });
}