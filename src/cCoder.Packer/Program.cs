// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace cCoder.Packer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        HostApplicationBuilder builder =
            Host.CreateApplicationBuilder(args: args);

        builder.Configuration
            .SetBasePath(basePath: AppContext.BaseDirectory)
            .AddJsonFile(path: "appsettings.json", optional: false);

        PackerConfiguration packerConfiguration =
            builder.Configuration.GetRequiredSection(key: "Packer")
                .Get<PackerConfiguration>()
            ?? throw new InvalidOperationException(
                message: "The Packer configuration is required.");

        builder.Services.AddPacker(configuration: packerConfiguration);

        using IHost host = builder.Build();

        return await host.Services.RunPackerAsync(args: args);
    }
}