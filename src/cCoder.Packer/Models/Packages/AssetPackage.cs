// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Packages;

public sealed record AssetPackage(
    string Name,
    string Description,
    string Category,
    string SourceApi,
    AssetPackageItem[] Items);