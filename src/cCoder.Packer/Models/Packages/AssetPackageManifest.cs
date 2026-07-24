// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Models.Packages;

public sealed record AssetPackageManifest(
    int SchemaVersion,
    AssetPackageManifestItem[] Packages);
