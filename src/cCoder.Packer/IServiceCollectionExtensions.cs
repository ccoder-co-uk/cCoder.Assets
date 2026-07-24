// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Configurations;
using cCoder.Packer.Services.Orchestrations.Commands;
using cCoder.Packer.Services.Processings.Commands;
using cCoder.Packer.Services.Processings.Packing;
using cCoder.Packer.Services.Processings.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace cCoder.Packer;

internal static class IServiceCollectionExtensions
{
    public static Task<int> RunPackerAsync(
        this IServiceProvider serviceProvider,
        string[] args) =>
        serviceProvider
            .GetRequiredService<ICommandResolvingOrchestrationService>()
            .ResolveAsync(args: args);

    public static IServiceCollection AddPacker(
        this IServiceCollection services,
        PackerConfiguration configuration) =>
        services
            .AddSingleton(implementationInstance: configuration)
            .AddSingleton<
                ICommandOptionsParserProcessingService,
                CommandOptionsParserProcessingService>()
            .AddSingleton<
                IConfiguredPathProcessingService,
                ConfiguredPathProcessingService>()
            .AddTransient<
                ICommandHandlerProviderProcessingService,
                CommandHandlerProviderProcessingService>()
            .AddTransient<
                IAssetReportProcessingService,
                AssetReportProcessingService>()
            .AddTransient<
                IPackageBuilderProcessingService,
                PackageBuilderProcessingService>()
            .AddTransient<
                IPackCommandHandlingOrchestrationService,
                PackCommandHandlingOrchestrationService>()
            .AddTransient<
                IReportCommandHandlingOrchestrationService,
                ReportCommandHandlingOrchestrationService>()
            .AddTransient<
                IUnpackCommandHandlingProcessingService,
                UnpackCommandHandlingProcessingService>()
            .AddKeyedTransient<ICommandHandlingService>(
                serviceKey: "pack",
                implementationFactory: (provider, _) =>
                    provider.GetRequiredService<
                        IPackCommandHandlingOrchestrationService>())
            .AddKeyedTransient<ICommandHandlingService>(
                serviceKey: "report",
                implementationFactory: (provider, _) =>
                    provider.GetRequiredService<
                        IReportCommandHandlingOrchestrationService>())
            .AddKeyedTransient<ICommandHandlingService>(
                serviceKey: "unpack",
                implementationFactory: (provider, _) =>
                    provider.GetRequiredService<
                        IUnpackCommandHandlingProcessingService>())
            .AddTransient<
                ICommandResolvingOrchestrationService,
                CommandResolvingOrchestrationService>();
}