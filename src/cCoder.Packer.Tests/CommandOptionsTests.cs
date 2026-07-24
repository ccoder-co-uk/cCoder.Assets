// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Services.Processings.Packing;

using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Tests;

public sealed partial class CommandOptionsTests
{
    [Fact]
    public void ShouldParseReportCommand()
    {
        // Given
        string[] args = ["-report"];

        // When
        CommandOptionsParserProcessingService service = new();

        CommandOptions options = service.Parse(args: args);

        // Then
        Assert.Equal(expected: "report", actual: options.Name);

        Assert.Null(@object: options.Target);

        Assert.Null(@object: options.Source);
    }

    [Fact]
    public void ShouldParseOptionalConfiguredPaths()
    {
        // Given
        string[] args =
        [
            "-report",
            "-dataPath", "CustomData",
            "-packagesPath", "CustomPackages",
        ];

        // When
        CommandOptionsParserProcessingService service = new();

        CommandOptions options = service.Parse(args: args);

        // Then
        Assert.Equal(expected: "CustomData", actual: options.DataPath);

        Assert.Equal(
            expected: "CustomPackages",
            actual: options.PackagesPath);
    }

    [Fact]
    public void ShouldParsePackCommand()
    {
        // Given
        string[] args =
        [
            "-pack",
            "-dataPath", "CustomData",
            "-packagesPath", "CustomPackages",
        ];

        // When
        CommandOptionsParserProcessingService service = new();

        CommandOptions options = service.Parse(args: args);

        // Then
        Assert.Equal(expected: "pack", actual: options.Name);

        Assert.Equal(expected: "CustomData", actual: options.DataPath);

        Assert.Equal(
            expected: "CustomPackages",
            actual: options.PackagesPath);
    }

    [Fact]
    public void ShouldParseCommonCacheCommand()
    {
        // Given
        string[] args =
        [
            "-unpack", "commoncache",
            "-from", "https://ccoder.co.uk/",
        ];

        // When
        CommandOptionsParserProcessingService service = new();

        CommandOptions options = service.Parse(args: args);

        // Then
        Assert.Equal(expected: "commoncache", actual: options.Target);

        Assert.Equal(
            expected: new Uri(uriString: "https://ccoder.co.uk/"),
            actual: options.Source);

        Assert.Null(@object: options.AppId);
    }

    [Fact]
    public void ShouldParseAppId()
    {
        // Given
        string[] args =
        [
            "-unpack", "app",
            "-from", "https://ccoder.co.uk",
            "-appId", "1",
        ];

        // When
        CommandOptionsParserProcessingService service = new();

        CommandOptions options = service.Parse(args: args);

        // Then
        Assert.Equal(expected: "app", actual: options.Target);

        Assert.Equal(expected: 1, actual: options.AppId);
    }
}