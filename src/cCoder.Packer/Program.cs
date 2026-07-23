using cCoder.Packer.Services.Orchestrations;

namespace cCoder.Packer.Exposures;

internal static class Program
{
    public static Task<int> Main(string[] args) =>
        PackerApplication.RunAsync(args);
}
