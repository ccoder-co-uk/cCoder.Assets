// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Dependencies;

namespace cCoder.Packer.Exposures;

internal static class Program
{
    public static Task<int> Main(string[] args) =>
        PackerApplicationDependency.RunAsync(args: args);
}