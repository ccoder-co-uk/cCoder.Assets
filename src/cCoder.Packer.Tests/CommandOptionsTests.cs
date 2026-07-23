// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Dependencies;
using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Tests;

public sealed partial class CommandOptionsTests
{
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
        CommandOptions options =
            CommandOptionsParserDependency.Parse(args: args);

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
        CommandOptions options =
            CommandOptionsParserDependency.Parse(args: args);

        // Then
        Assert.Equal(expected: "app", actual: options.Target);
        Assert.Equal(expected: 1, actual: options.AppId);
    }
}