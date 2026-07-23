using cCoder.Packer.Models;

namespace cCoder.Packer.Tests;

public sealed class CommandOptionsTests
{
    [Fact]
    public void ShouldParseCommonCacheCommand()
    {
        CommandOptions options = CommandOptions.Parse(
        [
            "-unpack", "commoncache",
            "-from", "https://ccoder.co.uk/",
        ]);

        Assert.Equal("commoncache", options.Target);
        Assert.Equal(new Uri("https://ccoder.co.uk/"), options.Source);
        Assert.Null(options.AppId);
    }

    [Fact]
    public void ShouldParseAppId()
    {
        CommandOptions options = CommandOptions.Parse(
        [
            "-unpack", "app",
            "-from", "https://ccoder.co.uk",
            "-appId", "1",
        ]);

        Assert.Equal("app", options.Target);
        Assert.Equal(1, options.AppId);
    }
}
