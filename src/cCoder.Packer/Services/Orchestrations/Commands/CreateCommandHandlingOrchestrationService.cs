// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;
using cCoder.Packer.Services.Processings.Provisioning;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Services.Orchestrations.Commands;

internal sealed partial class CreateCommandHandlingOrchestrationService(
    ICreateAppProcessingService createAppService,
    IConfiguredPathProcessingService configuredPathService)
    : ICreateCommandHandlingOrchestrationService
{
    public Task<int> HandleCommandOptionsAsync(
        CommandOptions command,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [command, cancellationToken]);

            Uri api = command.Source
                ?? throw new ArgumentException(
                    message: "The create API is required.");

            string name = Required(
                value: command.AppName,
                optionName: "name");

            string tenantId = Required(
                value: command.TenantId,
                optionName: "tenant");

            string user = Required(
                value: command.User,
                optionName: "user");

            string password = Required(
                value: command.Password,
                optionName: "pass");

            string baselinePath =
                configuredPathService.ResolveBaselinePath(
                    suppliedPath: Required(
                    value: command.BaselinePath,
                    optionName: "baseline"));

            int appId = await createAppService.ProvisionAppAsync(
                api: api,
                name: name,
                tenantId: tenantId,
                user: user,
                password: password,
                baselinePath: baselinePath,
                cancellationToken: cancellationToken);

            Console.WriteLine(
                value: $"Created app '{name}' with ID {appId} " +
                    $"and imported baseline '{baselinePath}'.");

            return 0;
        });

    private static string Required(
        string? value,
        string optionName) =>
        !string.IsNullOrWhiteSpace(value: value)
            ? value
            : throw new ArgumentException(
                message: $"The '-{optionName}' option is required.");
}