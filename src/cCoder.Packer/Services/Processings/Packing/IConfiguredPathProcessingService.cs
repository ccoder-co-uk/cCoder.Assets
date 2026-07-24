// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Processings.Packing;

internal interface IConfiguredPathProcessingService
{
    string ResolveDataPath(string? suppliedPath);

    string ResolvePackagesPath(string? suppliedPath);

    string ResolveBaselinePath(string? suppliedPath);
}